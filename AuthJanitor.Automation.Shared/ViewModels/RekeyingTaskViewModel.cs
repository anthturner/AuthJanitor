using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared.ViewModels
{
    public class RekeyingTaskViewModel : IAuthJanitorViewModel
    {
        public Guid ObjectId { get; set; }

        public DateTimeOffset Queued { get; set; }
        public DateTimeOffset Expiry { get; set; }

        public bool RekeyingInProgress { get; set; } = false;
        public bool RekeyingCompleted { get; set; } = false;
        public string RekeyingErrorMessage { get; set; } = string.Empty;

        public TaskConfirmationStrategies ConfirmationType { get; set; }

        public string PersistedCredentialUser { get; set; }

        public Guid AvailabilityScheduleId { get; set; }

        public IEnumerable<ManagedSecretViewModel> ManagedSecrets { get; set; }
    }
}
