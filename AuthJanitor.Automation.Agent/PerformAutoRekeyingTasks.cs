using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AuthJanitor.Automation.Agent
{
    public class PerformAutoRekeyingTasks : ProviderIntegratedFunction
    {
        public PerformAutoRekeyingTasks(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, INotificationProvider notificationProvider, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, notificationProvider, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [FunctionName("PerformAutoRekeyingTasks")]
        public async Task Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var toRekey = await RekeyingTasks.GetAsync(t =>
                (t.ConfirmationType == TaskConfirmationStrategies.AdminCachesSignOff ||
                 t.ConfirmationType == TaskConfirmationStrategies.AutomaticRekeyingAsNeeded ||
                 t.ConfirmationType == TaskConfirmationStrategies.AutomaticRekeyingScheduled) &&
                DateTimeOffset.UtcNow + TimeSpan.FromHours(ServiceConfiguration.AutomaticRekeyableJustInTimeLeadTimeHours) > t.Expiry);

            foreach (var task in toRekey)
            {
                task.RekeyingInProgress = true;
                await RekeyingTasks.UpdateAsync(task);

                string result;
                try
                {
                    result = await ExecuteRekeyingWorkflow(log, task);
                } catch (Exception ex)
                {
                    result = ex.Message;
                }

                task.RekeyingInProgress = false;
                if (result != string.Empty)
                    task.RekeyingErrorMessage = result;
                else
                    task.RekeyingCompleted = true;
                await RekeyingTasks.UpdateAsync(task);
            }
        }
    }
}
