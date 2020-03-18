using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
        public RekeyingTasks(INotificationProvider notificationProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(notificationProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks")] string[] secretIds,
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating new Task.");

            if (secretIds.Any(id => !ManagedSecrets.ContainsId(Guid.Parse(id))) ||
                ManagedSecrets.Get(s => secretIds.Contains(s.ObjectId.ToString())).Any(s =>
                    !s.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminCachesSignOff) &&
                    !s.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminSignsOffJustInTime)))
            {
                return new BadRequestErrorMessageResult("Invalid Managed Secret ID in set");
            }

            var earliestExpiry = ManagedSecrets.Get(s => secretIds.Contains(s.ObjectId.ToString()))
                .Min(s => s.LastChanged + s.ValidPeriod);

            RekeyingTask newTask = new RekeyingTask()
            {
                Queued = DateTimeOffset.Now,
                Expiry = earliestExpiry,
                ManagedSecretIds = secretIds.Select(s => Guid.Parse(s)).ToList()
            };

            await RekeyingTasks.InitializeAsync();
            RekeyingTasks.Create(newTask);
            await RekeyingTasks.CommitAsync();

            return new OkObjectResult(newTask);
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("List all Tasks.");

            return new OkObjectResult(RekeyingTasks.List().Select(t => GetViewModel(t)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Preview Actions for Task ID {0}.", taskId);

            return new OkObjectResult(GetViewModel(RekeyingTasks.Get(taskId)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tasks/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Deleting Task ID {0}.", taskId);

            if (!ManagedSecrets.ContainsId(taskId))
                return new BadRequestErrorMessageResult("Task not found!");

            RekeyingTasks.Delete(taskId);
            await RekeyingTasks.CommitAsync();
            return new OkResult();
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Approve")]
        public async Task<IActionResult> Approve(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks/{taskId:guid}/approve")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Administrator approved Task ID {0}", taskId);

            RekeyingTask task = RekeyingTasks.Get(taskId);
            Dictionary<Guid, string> secretResults = new Dictionary<Guid, string>();

            if (task.Expiry < DateTime.Now)
            {
                log.LogError("Expiry time has passed; this rekeying operation may be a little bumpy!");
            }

            var allResourceIds = Resources.List().Select(r => r.ObjectId);
            foreach (Guid managedSecretId in task.ManagedSecretIds)
            {
                try
                {
                    log.LogInformation("Rekeying Managed Secret ID {0}", managedSecretId);
                    ManagedSecret secret = ManagedSecrets.Get(managedSecretId);

                    if (secret.ResourceIds.Any(id => !allResourceIds.Contains(id)))
                    {
                        return new BadRequestErrorMessageResult("Invalid Resource ID in set");
                    }

                    var providers =
                        secret.ResourceIds.Select(id => Resources.Get(id))
                                          .Select(r =>
                                          {
                                              var provider = GetProvider(r.ProviderType);
                                              provider.SerializedConfiguration = r.ProviderConfiguration;
                                              return provider;
                                          }).ToArray();

                    await HelperMethods.RunRekeyingWorkflow(log, secret.ValidPeriod, providers);
                    secretResults.Add(managedSecretId, "Success");
                }
                catch (Exception ex)
                {
                    secretResults.Add(managedSecretId, $"Error: {ex.Message}");
                }
            }

            return new OkObjectResult(secretResults);
        }
    }
}
