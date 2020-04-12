using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
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
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.Agent
{
    public class ExternalSignal : ProviderIntegratedFunction
    {
        private const int RETURN_NO_CHANGE = 0;
        private const int RETURN_CHANGE_OCCURRED = 1;
        private const int RETURN_RETRY_SHORTLY = 2;

        private const int MAX_EXECUTION_SECONDS_BEFORE_RETRY = 30;

        public ExternalSignal(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, EventDispatcherService eventDispatcherService, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, RekeyingAttemptLogger, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, eventDispatcherService, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, scheduleViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [FunctionName("ExternalSignal")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secrets/{managedSecretId:guid}/{nonce}")] HttpRequest req,
            Guid managedSecretId,
            string nonce,
            ILogger log)
        {
            _ = req; // unused but required for attribute

            log.LogInformation("External signal called to check ManagedSecret ID {0} against nonce {1}", managedSecretId, nonce);

            var secret = await ManagedSecrets.GetAsync(managedSecretId);
            if (secret == null)
                return new BadRequestErrorMessageResult("Invalid ManagedSecret ID");
            if (!secret.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.ExternalSignal))
                return new BadRequestErrorMessageResult("This ManagedSecret cannot be used with External Signals");

            if ((await RekeyingTasks.GetAsync(t => t.ManagedSecretId == secret.ObjectId))
                                    .Any(t => t.RekeyingInProgress))
            {
                return new OkObjectResult(RETURN_RETRY_SHORTLY);
            }

            if ((secret.IsValid && secret.TimeRemaining <= TimeSpan.FromHours(ServiceConfiguration.ExternalSignalRekeyableLeadTimeHours)) || !secret.IsValid)
            {
                var rekeyingTask = new Task(async () =>
                    {
                        var task = new RekeyingTask()
                        {
                            ManagedSecretId = secret.ObjectId,
                            Expiry = secret.Expiry,
                            Queued = DateTimeOffset.UtcNow,
                            RekeyingInProgress = true
                        };
                        await RekeyingTasks.CreateAsync(task);

                        var rekeyingAttemptLogger = new RekeyingAttemptLogger(log)
                        {
                            UserDisplayName = "Agent Identity",
                            UserEmail = string.Empty
                        };
                        try
                        {
                            await ExecuteRekeyingWorkflow(task, rekeyingAttemptLogger);
                        }
                        catch (Exception ex)
                        {
                            rekeyingAttemptLogger.OuterException = JsonConvert.SerializeObject(ex, Formatting.Indented);
                        }

                        task.RekeyingInProgress = false;
                        task.RekeyingCompleted = rekeyingAttemptLogger.IsSuccessfulAttempt;
                        task.RekeyingFailed = !rekeyingAttemptLogger.IsSuccessfulAttempt;
                        task.Attempts.Add(rekeyingAttemptLogger);

                        await RekeyingTasks.UpdateAsync(task);

                        if (task.RekeyingCompleted)
                            await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.RotationTaskCompletedAutomatically, nameof(Agent.ExternalSignal.Run), task);
                        else
                            await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.RotationTaskAttemptFailed, nameof(Agent.ExternalSignal.Run), task);
                    },
                    TaskCreationOptions.LongRunning);
                rekeyingTask.Start();

                if (!rekeyingTask.Wait(TimeSpan.FromSeconds(MAX_EXECUTION_SECONDS_BEFORE_RETRY)))
                {
                    log.LogInformation("Rekeying workflow was started but exceeded the maximum request time! ({0})", TimeSpan.FromSeconds(MAX_EXECUTION_SECONDS_BEFORE_RETRY));
                    return new OkObjectResult(RETURN_RETRY_SHORTLY);
                }
                else
                {
                    log.LogInformation("Completed rekeying workflow within maximum time! ({0})", TimeSpan.FromSeconds(MAX_EXECUTION_SECONDS_BEFORE_RETRY));
                    return new OkObjectResult(RETURN_CHANGE_OCCURRED);
                }
            }
            return new OkObjectResult(RETURN_NO_CHANGE);
        }
    }
}
