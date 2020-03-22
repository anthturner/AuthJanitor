using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi
{
    /// <summary>
    /// API functions to control the creation management, and approval of Rekeying Tasks.
    /// A Rekeying Task is a time-bounded description of one or more Managed Secrets to be rekeyed.
    /// </summary>
    public class RekeyingTasks : ProviderIntegratedFunction
    {
        public RekeyingTasks(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, INotificationProvider notificationProvider, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, notificationProvider, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks")] string secretId,
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating new Task.");

            if (!await ManagedSecrets.ContainsIdAsync(Guid.Parse(secretId)))
                return new BadRequestErrorMessageResult("Invalid Managed Secret ID");
            
            var secret = await ManagedSecrets.GetAsync(Guid.Parse(secretId));
            if (!secret.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminCachesSignOff) &&
                !secret.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminSignsOffJustInTime))
                return new BadRequestErrorMessageResult("Managed Secret does not support administrator approval!");

            RekeyingTask newTask = new RekeyingTask()
            {
                Queued = DateTimeOffset.UtcNow,
                Expiry = secret.Expiry,
                ManagedSecretId = secret.ObjectId
            };

            await RekeyingTasks.CreateAsync(newTask);

            return new OkObjectResult(newTask);
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("List all Tasks.");

            return new OkObjectResult((await RekeyingTasks.ListAsync()).Select(t => GetViewModel(t)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Preview Actions for Task ID {0}.", taskId);

            return new OkObjectResult(GetViewModel((await RekeyingTasks.GetAsync(taskId))));
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tasks/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Deleting Task ID {0}.", taskId);

            if (!await RekeyingTasks.ContainsIdAsync(taskId))
                return new BadRequestErrorMessageResult("Task not found!");

            await RekeyingTasks.DeleteAsync(taskId);
            return new OkResult();
        }

        [RegisterOBOCredential]
        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Approve")]
        public async Task<IActionResult> Approve(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks/{taskId:guid}/approve")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Administrator approved Task ID {0}", taskId);
            var task = await RekeyingTasks.GetAsync(taskId);

            if (task.ConfirmationType == TaskConfirmationStrategies.AdminCachesSignOff)
            {
                var credential = CredentialProvider.Get(MultiCredentialProvider.CredentialType.UserCredential);
                var persisted = await SecureStorageProvider.Persist<string>(credential.Expiry, credential.AccessToken);
                task.PersistedCredentialId = persisted;
                task.PersistedCredentialUser = credential.Username;
                await RekeyingTasks.UpdateAsync(task);
                return new OkResult();
            }
            else if (task.ConfirmationType == TaskConfirmationStrategies.AdminSignsOffJustInTime)
            {
                task.RekeyingInProgress = true;
                await RekeyingTasks.UpdateAsync(task);

                var result = await ExecuteRekeyingWorkflow(log, task);

                task.RekeyingInProgress = false;
                if (result != string.Empty)
                    task.RekeyingErrorMessage = result;
                else
                    task.RekeyingCompleted = true;
                await RekeyingTasks.UpdateAsync(task);

                if (result != string.Empty)
                    return new BadRequestErrorMessageResult(result);
                else
                    return new OkResult();
            }
            else
            {
                log.LogError("Task does not support an Administrator's approval!");
                return new BadRequestErrorMessageResult("Task does not support an Administrator's approval!");
            }
        }
    }
}
