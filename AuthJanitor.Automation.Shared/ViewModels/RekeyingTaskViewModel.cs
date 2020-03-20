using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared.ViewModels
{
    public class RekeyingTaskViewModel : IAuthJanitorViewModel
    {
        public Guid ObjectId { get; set; }

        public DateTimeOffset Queued { get; set; }
        public DateTimeOffset Expiry { get; set; }

        public IEnumerable<ManagedSecretViewModel> ManagedSecrets { get; set; }
    }
}
