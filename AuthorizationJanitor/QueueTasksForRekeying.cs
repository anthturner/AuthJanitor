using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public static class QueueTasksForRekeying
    {
        private const int NUMBER_OF_HOURS_LEAD_TIME = 48;

        [FunctionName("QueueTasksForRekeying")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var taskStore = new QueuedRekeyingTaskStore();
            var secretStore = new ManagedSecretStore();
            var tasks = await taskStore.GetTasks();
            var secrets = await secretStore.GetManagedSecrets();

            var candidates = secrets.Where(s => s.LastChanged + s.ValidPeriod > DateTime.Now - TimeSpan.FromHours(NUMBER_OF_HOURS_LEAD_TIME)).ToList();
            log.LogInformation("{0} candidates are expiring soon!", candidates.Count);
            var newTasks = new List<QueuedRekeyingTask>();
            foreach (var candidate in candidates)
            {
                if (tasks.Any(t => t.ManagedSecretIds.Contains(candidate.ManagedSecretId)))
                    continue;

                log.LogInformation("Creating rekeying task for Managed Secret ID {0}, which expires at {1}", candidate.LastChanged + candidate.ValidPeriod);
                await taskStore.Enqueue(new QueuedRekeyingTask()
                {
                    ManagedSecretIds = new List<Guid>() { candidate.ManagedSecretId },
                    DropDead = candidate.LastChanged + candidate.ValidPeriod,
                    Queued = DateTime.Now,
                    TaskId = Guid.NewGuid()
                });
            }
        }
    }
}
