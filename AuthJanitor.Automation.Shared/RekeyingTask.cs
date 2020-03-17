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

        public IList<Guid> ManagedSecretIds { get; set; }
    }
}
