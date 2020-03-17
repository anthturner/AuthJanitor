using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor
{
    public class QueueTasksForRekeying : ProviderIntegratedFunction
    {
        public QueueTasksForRekeying(INotificationProvider notificationProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(notificationProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        protected TimeSpan LeadTime => TimeSpan.FromHours(double.Parse(Environment.GetEnvironmentVariable("SecretExpiryLeadTimeHours", EnvironmentVariableTarget.Process)));
        protected string[] AdminEmails => Environment.GetEnvironmentVariable("AdminEmails", EnvironmentVariableTarget.Process).Split(';');

        [FunctionName("QueueTasksForRekeying")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("Checking for necessary rekeyings... (lead time: {0})", LeadTime);

            var existingTasks = await RekeyingTasks.List();
            var allSecretsToBeChecked = (await ManagedSecrets.List())
                .Where(s => s.TimeRemaining <= LeadTime)
                .Where(s => !existingTasks.Any(t => t.ManagedSecretIds.Contains(s.ObjectId)));

            if (!allSecretsToBeChecked.Any())
            {
                log.LogInformation("No rekeying tasks need to be generated.");
                return;
            }
            log.LogInformation("Executing scheduled actions on {0} ManagedSecrets.", allSecretsToBeChecked.Count());

            // We prefer Service Principal/MSI approval if the RekeyingTask is being automatically generated.
            // Since the Admin can create a RekeyingTask manually at any time, it's assumed that if we have
            //  entered the lead time period prior to expiry, automated rekeying should occur if possible.
            await CreateAdminApprovalRekeyingTasks(log, allSecretsToBeChecked
                        .Where(s => s.TaskConfirmationStrategies.UsesOBOTokens() &&
                                    !s.TaskConfirmationStrategies.UsesServicePrincipal()));

            await ExecuteRekeyingWorkflowForMSISecrets(log, allSecretsToBeChecked
                        .Where(s => s.TaskConfirmationStrategies.UsesServicePrincipal()));
        }

        private async Task CreateAdminApprovalRekeyingTasks(ILogger log, IEnumerable<ManagedSecret> secrets)
        {
            if (!secrets.Any())
            {
                log.LogInformation("No tasks to create for administrator approval.");
                return;
            }

            log.LogInformation("Creating {0} tasks for administrator approval", secrets.Count());
            var newTasks = new List<RekeyingTask>();
            foreach (var secret in secrets)
            {
                var newTask = new RekeyingTask()
                {
                    ManagedSecretIds = new List<Guid>() { secret.ObjectId },
                    Expiry = secret.Expiry,
                    Queued = DateTimeOffset.Now,
                    RekeyingInProgress = false
                };
                newTasks.Add(newTask);
                await RekeyingTasks.Create(newTask);
            }
            await RekeyingTasks.Commit();

            await Task.WhenAll(newTasks.Select(t => NotificationProvider.DispatchNotification_AdminApprovalRequiredTaskCreated(AdminEmails, t)));
        }

        private async Task ExecuteRekeyingWorkflowForMSISecrets(ILogger log, IEnumerable<ManagedSecret> secrets)
        {
            if (!secrets.Any())
            {
                log.LogInformation("No ManagedSecrets to rekey with SP/MSI.");
                return;
            }

            var resources = await Resources.List();
            log.LogInformation("Rekeying {0} ManagedSecrets with Service Principal/MSI.", secrets.Count());
            foreach (var secret in secrets)
            {
                log.LogInformation("Beginning rekeying for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);

                log.LogDebug("Running access sanity check on {0} Resources associated with ManagedSecret", secret.ResourceIds.Count());
                var testResults = new Dictionary<Guid, bool>();

                // Run all tests in parallel to save time!
                await Task.WhenAll(resources.Where(r => secret.ResourceIds.Contains(r.ObjectId))
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
                    log.LogCritical("Failed to run sanity checks; skipping this secret!");
                    continue;
                }

                await HelperMethods.RunRekeyingWorkflow(log, secret.ValidPeriod,
                    resources.Where(r => secret.ResourceIds.Contains(r.ObjectId))
                             .Select(r => GetProvider(r.ProviderType, r.ProviderConfiguration, MultiCredentialProvider.CredentialType.AgentServicePrincipal))
                             .ToArray());

                log.LogInformation("Completed rekeying workflow for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);
            }
        }
    }
}
