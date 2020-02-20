using Newtonsoft.Json;
using System;

namespace AuthorizationJanitor
{
    public class ManagedSecret
    {
        public Guid ManagedSecretId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public DateTime LastChanged { get; set; }
        public TimeSpan ValidPeriod { get; set; }

        /// <summary>
        /// If the ManagedSecret is valid
        /// </summary>
        [JsonIgnore]
        public bool IsValid => LastChanged + ValidPeriod < DateTime.Now;

        public Type ConsumingApplicationType { get; set; }
        public Type RekeyableServiceType { get; set; }

        public string ConsumingApplicationConfiguration { get; set; }
        public string RekeyableServiceConfiguration { get; set; }

        public string Nonce { get; set; }
    }
}
