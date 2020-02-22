using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public class RekeyingTask
    {
        public Guid RekeyingTaskId { get; set; }

        public DateTime Queued { get; set; }
        public DateTime Expiry { get; set; }

        public IList<Guid> ManagedSecretIds { get; set; }
    }
}
