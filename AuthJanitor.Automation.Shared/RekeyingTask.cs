﻿using AuthJanitor.Providers;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public class RekeyingTask : IDataStoreCompatibleStructure
    {
        public Guid ObjectId { get; set; } = Guid.NewGuid();

        public DateTimeOffset Queued { get; set; }
        public DateTimeOffset Expiry { get; set; }

        public bool RekeyingInProgress { get; set; } = false;
        public bool RekeyingCompleted { get; set; } = false;
        public bool RekeyingFailed { get; set; } = false;

        public TaskConfirmationStrategies ConfirmationType { get; set; }

        public string PersistedCredentialUser { get; set; }
        public Guid PersistedCredentialId { get; set; }

        public Guid AvailabilityScheduleId { get; set; }

        public Guid ManagedSecretId { get; set; }

        public List<RekeyingAttemptLogger> Attempts { get; set; } = new List<RekeyingAttemptLogger>();
    }
}
