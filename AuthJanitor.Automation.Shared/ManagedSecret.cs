using AuthJanitor.Providers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public class ManagedSecret : IDataStoreCompatibleStructure
    {
        public const int DEFAULT_NONCE_LENGTH = 64;

        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public DateTime LastChanged { get; set; }
        public TimeSpan ValidPeriod { get; set; }
        
        public string Nonce { get; set; }

        /// <summary>
        /// If the ManagedSecret is valid
        /// </summary>
        [JsonIgnore]
        public bool IsValid => LastChanged + ValidPeriod < DateTime.Now;

        public ManagedSecret()
        {
            ObjectId = Guid.NewGuid();
            LastChanged = DateTime.MinValue;
            Nonce = HelperMethods.GenerateCryptographicallySecureString(DEFAULT_NONCE_LENGTH);
        }

        public List<Guid> ResourceIds { get; set; } = new List<Guid>();
    }
}
