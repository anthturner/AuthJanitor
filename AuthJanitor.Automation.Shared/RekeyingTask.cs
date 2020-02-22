using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public class RekeyingTask : IDataStoreCompatibleStructure
    {
        public Guid ObjectId { get; set; }

        public DateTimeOffset Queued { get; set; }
        public DateTimeOffset Expiry { get; set; }

        public IList<Guid> ManagedSecretIds { get; set; }

        public RekeyingTask() => ObjectId = Guid.NewGuid();
    }
}
