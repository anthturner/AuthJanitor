using AuthJanitor.Automation.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthJanitor
{
    public static class QueueTasksForRekeying
    {
        private const int NUMBER_OF_HOURS_LEAD_TIME = 48;

        [FunctionName("QueueTasksForRekeying")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Checking for necessary rekeyings...");

            CloudBlobDirectory taskStoreDirectory = null;
            CloudBlobDirectory secretsDirectory = null;
            IDataStore<RekeyingTask> taskStore = new BlobDataStore<RekeyingTask>(taskStoreDirectory);
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretsDirectory);

            IList<ManagedSecret> candidates = await secretStore.Get(s => s.LastChanged + s.ValidPeriod > DateTime.Now - TimeSpan.FromHours(NUMBER_OF_HOURS_LEAD_TIME));
            log.LogInformation("{0} candidates are expiring soon!", candidates.Count);
            List<RekeyingTask> newTasks = new List<RekeyingTask>();
            foreach (ManagedSecret candidate in candidates)
            {
                if (await taskStore.Get(t => t.ManagedSecretIds.Contains(candidate.ObjectId)) != null)
                {
                    continue;
                }

                log.LogInformation("Creating rekeying task for Managed Secret ID {0}, which expires at {1}", candidate.LastChanged + candidate.ValidPeriod);
                await taskStore.Create(new RekeyingTask()
                {
                    ManagedSecretIds = new List<Guid>() { candidate.ObjectId },
                    Expiry = candidate.LastChanged + candidate.ValidPeriod,
                    Queued = DateTimeOffset.Now
                });
            }
        }
    }
}
