using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor
{
    public static class QueueTasksForRekeying
    {
        private const int NUMBER_OF_HOURS_LEAD_TIME = 48;

        public static async Task RotateKey()
        {
            HelperMethods.InitializeServiceProvider(new LoggerFactory());

            var rekeyableProvider = HelperMethods.ServiceProvider.GetService(
                typeof(Providers.Storage.StorageAccountRekeyableObjectProvider)) as Providers.Storage.StorageAccountRekeyableObjectProvider;
            rekeyableProvider.Configuration = new Providers.Storage.StorageAccountKeyConfiguration()
            {
                KeyType = Providers.Storage.StorageAccountKeyConfiguration.StorageKeyTypes.Key1,
                ResourceGroup = "resource_group",
                ResourceName = "resource_name",
                SkipScramblingOtherKey = false
            };

            var appProvider = HelperMethods.ServiceProvider.GetService(
                typeof(Providers.AppServices.WebApps.AppSettingsWebAppApplicationLifecycleProvider)) as Providers.AppServices.WebApps.AppSettingsWebAppApplicationLifecycleProvider;
            appProvider.Configuration = new Providers.AppServices.AppSettingConfiguration()
            {
                ResourceGroup = "resource_group",
                ResourceName = "resource_name",
                SettingName = "storage_key",
                SourceSlot = "production",
                TemporarySlot = "temporary",
                DestinationSlot = "production"
            };

            await HelperMethods.RunRekeyingWorkflow(TimeSpan.FromDays(7), rekeyableProvider, appProvider);
        }

        [FunctionName("QueueTasksForRekeying")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var taskStore = new RekeyingTaskStore();
            var secretStore = new ManagedSecretStore();
            var tasks = await taskStore.GetTasks();
            var secrets = await secretStore.GetManagedSecrets();

            var candidates = secrets.Where(s => s.LastChanged + s.ValidPeriod > DateTime.Now - TimeSpan.FromHours(NUMBER_OF_HOURS_LEAD_TIME)).ToList();
            log.LogInformation("{0} candidates are expiring soon!", candidates.Count);
            var newTasks = new List<RekeyingTask>();
            foreach (var candidate in candidates)
            {
                if (tasks.Any(t => t.ManagedSecretIds.Contains(candidate.ManagedSecretId)))
                    continue;

                log.LogInformation("Creating rekeying task for Managed Secret ID {0}, which expires at {1}", candidate.LastChanged + candidate.ValidPeriod);
                await taskStore.Enqueue(new RekeyingTask()
                {
                    ManagedSecretIds = new List<Guid>() { candidate.ManagedSecretId },
                    Expiry = candidate.LastChanged + candidate.ValidPeriod,
                    Queued = DateTime.Now,
                    RekeyingTaskId = Guid.NewGuid()
                });
            }
        }
    }
}
