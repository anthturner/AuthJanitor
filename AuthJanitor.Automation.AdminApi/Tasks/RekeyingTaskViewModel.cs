using AuthJanitor.Automation.AdminApi.ManagedSecrets;
using AuthJanitor.Automation.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi.Tasks
{
    public class RekeyingTaskViewModel
    {
        public Guid ObjectId { get; set; }

        public DateTimeOffset Queued { get; set; }
        public DateTimeOffset Expiry { get; set; }

        public IDictionary<Guid, string> ManagedSecretNames { get; set; }
        public IList<ManagedSecretViewModel> ManagedSecrets { get; set; }

        public static RekeyingTaskViewModel FromRekeyingTask(RekeyingTask rekeyingTask, List<ManagedSecret> secrets)
        {
            return new RekeyingTaskViewModel()
            {
                ObjectId = rekeyingTask.ObjectId,
                Queued = rekeyingTask.Queued,
                Expiry = rekeyingTask.Expiry,
                ManagedSecretNames = secrets.Where(s => rekeyingTask.ManagedSecretIds.Contains(s.ObjectId))
                                            .ToDictionary(k => k.ObjectId, v => v.Name)
            };
        }

        public static RekeyingTaskViewModel FromRekeyingTaskWithSecrets(RekeyingTask rekeyingTask, List<ManagedSecret> secrets, List<Resource> resources)
        {
            return new RekeyingTaskViewModel()
            {
                ObjectId = rekeyingTask.ObjectId,
                Queued = rekeyingTask.Queued,
                Expiry = rekeyingTask.Expiry,
                ManagedSecrets = secrets.Where(s => rekeyingTask.ManagedSecretIds.Contains(s.ObjectId))
                                        .Select(s => ManagedSecretViewModel.FromManagedSecret(s, resources))
                                        .ToList()
            };
        }
    }
}
