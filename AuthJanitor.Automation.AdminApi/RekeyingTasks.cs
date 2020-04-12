using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
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
        public RekeyingTasks(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, EventDispatcherService eventDispatcherService, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, RekeyingAttemptLogger, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, eventDispatcherService, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, scheduleViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks/{secretId:guid}")] Guid secretId,
            HttpRequest req)
        {
            if (!req.IsValidUser(AuthJanitorRoles.ServiceOperator, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            if (!await ManagedSecrets.ContainsIdAsync(secretId))
            {
                await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.RekeyingTasks.Create), "Secret ID not found");
                return new NotFoundObjectResult("Secret not found!");
            }

            var secret = await ManagedSecrets.GetAsync(secretId);
            if (!secret.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminCachesSignOff) &&
                !secret.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminSignsOffJustInTime))
            {
                await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.ManagedSecrets.Create), "Managed Secret does not support adminstrator approval");
                return new BadRequestErrorMessageResult("Managed Secret does not support administrator approval!");
            }

            RekeyingTask newTask = new RekeyingTask()
            {
                Queued = DateTimeOffset.UtcNow,
                Expiry = secret.Expiry,
                ManagedSecretId = secret.ObjectId
            };

            await RekeyingTasks.CreateAsync(newTask);

            await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.RotationTaskCreatedForApproval, nameof(AdminApi.ManagedSecrets.Create), newTask);

            return new OkObjectResult(newTask);
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks")] HttpRequest req)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            return new OkObjectResult((await RekeyingTasks.ListAsync()).Select(t => GetViewModel(t)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks/{taskId:guid}")] HttpRequest req,
            Guid taskId)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            if (!await RekeyingTasks.ContainsIdAsync(taskId))
            {
                await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.RekeyingTasks.Get), "Rekeying Task not found");
                return new NotFoundResult();
            }

            return new OkObjectResult(GetViewModel((await RekeyingTasks.GetAsync(taskId))));
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tasks/{taskId:guid}")] HttpRequest req,
            Guid taskId)
        {
            if (!req.IsValidUser(AuthJanitorRoles.ServiceOperator, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            if (!await RekeyingTasks.ContainsIdAsync(taskId))
            {
                await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.RekeyingTasks.Delete), "Rekeying Task not found");
                return new NotFoundResult();
            }

            await RekeyingTasks.DeleteAsync(taskId);

            await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.RotationTaskDeleted, nameof(AdminApi.RekeyingTasks.Delete), taskId);

            return new OkResult();
        }

        [ProtectedApiEndpoint]
        [FunctionName("RekeyingTasks-Approve")]
        public async Task<IActionResult> Approve(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks/{taskId:guid}/approve")] HttpRequest req,
            Guid taskId,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            if (!req.IsValidUser(AuthJanitorRoles.ServiceOperator, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            log.LogInformation("Administrator approved Task ID {0}", taskId);

            var task = await RekeyingTasks.GetAsync(taskId);

            log.LogInformation("Registering incoming user credential...");
            try { await RegisterUserCredential(req); }
            catch (Exception ex)
            {
                var logger = new RekeyingAttemptLogger();
                logger.LogCritical(ex, "Error registering user credential!");
                task.Attempts.Add(logger);
                await RekeyingTasks.UpdateAsync(task);
                return new BadRequestErrorMessageResult(ex.Message);
            }

            if (task.ConfirmationType == TaskConfirmationStrategies.AdminCachesSignOff)
            {
                var credential = CredentialProvider.Get(MultiCredentialProvider.CredentialType.UserCredential);
                var persisted = await SecureStorageProvider.Persist<Azure.Core.AccessToken>(
                    credential.Expiry,
                    new Azure.Core.AccessToken(credential.AccessToken, credential.Expiry));
                task.PersistedCredentialId = persisted;
                task.PersistedCredentialUser = credential.Username;
                await RekeyingTasks.UpdateAsync(task);
                return new OkResult();
            }
            else if (task.ConfirmationType == TaskConfirmationStrategies.AdminSignsOffJustInTime)
            {
                task.RekeyingInProgress = true;
                await RekeyingTasks.UpdateAsync(task);

                var aggregatedStringLogger = new RekeyingAttemptLogger(log)
                {
                    UserDisplayName =
                        claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value +
                        " " +
                        claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value,
                    UserEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value
                };
                try
                {
                    await ExecuteRekeyingWorkflow(task, aggregatedStringLogger);
                    task.RekeyingInProgress = false;
                    task.RekeyingCompleted = aggregatedStringLogger.IsSuccessfulAttempt;
                    task.RekeyingFailed = !aggregatedStringLogger.IsSuccessfulAttempt;
                }
                catch (Exception ex)
                {
                    task.RekeyingInProgress = false;
                    task.RekeyingCompleted = false;
                    task.RekeyingFailed = true;
                    if (aggregatedStringLogger.OuterException == null)
                        aggregatedStringLogger.OuterException = JsonConvert.SerializeObject(ex, Formatting.Indented);
                }

                task.Attempts.Add(aggregatedStringLogger);
                await RekeyingTasks.UpdateAsync(task);

                if (task.RekeyingCompleted)
                    await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.RotationTaskCompletedManually, nameof(AdminApi.RekeyingTasks.Approve), task);
                else
                    await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.RotationTaskAttemptFailed, nameof(AdminApi.RekeyingTasks.Approve), task);

                if (task.RekeyingFailed)
                    return new BadRequestErrorMessageResult(aggregatedStringLogger.OuterException);
                else
                    return new OkResult();
            }
            else
            {
                log.LogError("Task does not support an Administrator's approval!");
                return new BadRequestErrorMessageResult("Task does not support an Administrator's approval!");
            }
        }

        private async Task RegisterUserCredential(HttpRequest request)
        {
            if (!request.Headers.ContainsKey("X-MS-TOKEN-AAD-ID-TOKEN"))
                return;

            var idToken = request.Headers["X-MS-TOKEN-AAD-ID-TOKEN"].FirstOrDefault();

            var client = new HttpClient();
            var expando = new System.Dynamic.ExpandoObject();
            var dict = new Microsoft.AspNetCore.Routing.RouteValueDictionary(new
            {
                grant_type = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                client_id = Environment.GetEnvironmentVariable("CLIENT_ID", EnvironmentVariableTarget.Process),
                client_secret = Environment.GetEnvironmentVariable("CLIENT_SECRET", EnvironmentVariableTarget.Process),
                assertion = request.Headers["X-MS-TOKEN-AAD-ID-TOKEN"].FirstOrDefault(),
                resource = "https://management.core.windows.net",
                requested_token_use = "on_behalf_of"
            });

            var result = await client.PostAsync("https://login.microsoftonline.com/" +
                Environment.GetEnvironmentVariable("TENANT_ID", EnvironmentVariableTarget.Process) +
                "/oauth2/token",
                new FormUrlEncodedContent(dict.ToDictionary(k => k.Key, v => v.Value as string).ToList()));
            var resultObj = JsonConvert.DeserializeObject<TokenExchangeResponse>(
                await result.Content.ReadAsStringAsync());

            CredentialProvider.Register(
                MultiCredentialProvider.CredentialType.UserCredential,
                resultObj.AccessToken,
                resultObj.ExpiresOnDateTime);
        }
    }
}
