using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using System.Collections.Generic;
using System.Web.Http;
using System.Linq;
using AuthJanitor.Automation.Shared.NotificationProviders;

namespace AuthJanitor.Automation.Agent
{
    public class ExternalSignal : ProviderIntegratedFunction
    {
        private const int RETURN_NO_CHANGE = 0;
        private const int RETURN_CHANGE_OCCURRED = 1;
        private const int RETURN_RETRY_SHORTLY = 2;

        private const int MAX_EXECUTION_SECONDS_BEFORE_RETRY = 30;

        public ExternalSignal(INotificationProvider notificationProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(notificationProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        /// <summary>
        /// The amount of time before the secret expires when it will be automatically changed if signalled
        /// </summary>
        protected TimeSpan SignalledChangeWindow => TimeSpan.FromHours(double.Parse(Environment.GetEnvironmentVariable("SignalledChangeWindow", EnvironmentVariableTarget.Process)));

        [FunctionName("ExternalSignal")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secrets/{managedSecretId:guid}/{nonce}")] HttpRequest req,
            Guid managedSecretId,
            string nonce,
            ILogger log)
        {
            log.LogInformation("External signal called to check ManagedSecret ID {0} against nonce {1}", managedSecretId,  nonce);

            var secret = ManagedSecrets.Get(managedSecretId);
            if (secret == null)
                return new BadRequestErrorMessageResult("Invalid ManagedSecret ID");
            if (!secret.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.ExternalSignal))
                return new BadRequestErrorMessageResult("This ManagedSecret cannot be used with External Signals");

            if (RekeyingTasks.Get(t => t.ManagedSecretIds.Contains(secret.ObjectId))
                             .Any(t => t.RekeyingInProgress))
            {
                return new OkObjectResult(RETURN_RETRY_SHORTLY);
            }

            if ((secret.IsValid && secret.TimeRemaining <= SignalledChangeWindow) || !secret.IsValid)
            {
                var temporaryTask = new RekeyingTask()
                {
                    ManagedSecretIds = new List<Guid>() { secret.ObjectId },
                    Expiry = secret.Expiry,
                    Queued = DateTimeOffset.Now,
                    RekeyingInProgress = true
                };
                RekeyingTasks.Create(temporaryTask);
                await RekeyingTasks.CommitAsync();

                try
                {
                    log.LogInformation("Rekeying ManagedSecret '{0}' (ID {1}) - {2} remaining", secret.Name, secret.ObjectId, secret.TimeRemaining);

                    log.LogDebug("Running access sanity check on {0} Resources associated with ManagedSecret", secret.ResourceIds.Count());
                    var testResults = new Dictionary<Guid, bool>();

                    // Run all tests in parallel to save time!
                    await Task.WhenAll(Resources.Get(r => secret.ResourceIds.Contains(r.ObjectId))
                                                .Select(r =>
                                                    GetProvider(
                                                        r.ProviderType,
                                                        r.ProviderConfiguration,
                                                        MultiCredentialProvider.CredentialType.AgentServicePrincipal)
                                                    .Test()
                                                    .ContinueWith(testTask =>
                                                    {
                                                        testResults[r.ObjectId] = testTask.Result;
                                                    })));

                    if (testResults.Any(r => !r.Value))
                    {
                        log.LogCritical("Failed to run sanity checks!");
                        return new OkObjectResult(RETURN_NO_CHANGE);
                    }

                    var rekeyingTask = new Task(async () =>
                        {
                            await HelperMethods.RunRekeyingWorkflow(log, secret.ValidPeriod,
                                  Resources.Get(r => secret.ResourceIds.Contains(r.ObjectId))
                                           .Select(r => GetProvider(r.ProviderType, r.ProviderConfiguration, MultiCredentialProvider.CredentialType.AgentServicePrincipal))
                                           .ToArray());
                            RekeyingTasks.Delete(temporaryTask.ObjectId);
                            await RekeyingTasks.CommitAsync();
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
                catch (Exception ex)
                {
                    log.LogCritical(ex, "Error executing a rekeying operation on ManagedSecret ID {0}", secret.ObjectId);
                    return new InternalServerErrorResult();
                }
            }
            return new OkObjectResult(RETURN_NO_CHANGE);
        }
    }
}
