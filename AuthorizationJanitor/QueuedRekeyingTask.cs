using System;
using System.Collections.Generic;

namespace AuthorizationJanitor
{
    public class QueuedRekeyingTask
    {
        public Guid TaskId { get; set; }
        public DateTime Queued { get; set; }
        public DateTime DropDead { get; set; }

        public IList<Guid> ManagedSecretIds { get; set; }
    }
}
