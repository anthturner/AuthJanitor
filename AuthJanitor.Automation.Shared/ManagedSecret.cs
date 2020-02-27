using AuthJanitor.Providers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    [Flags]
    public enum TaskConfirmationStrategies : int
    {
        None = 0, // fa fa-close
        AdminSignsOffJustInTime = 1, // fa fa-pencil
        AdminCachesSignOff = 2, // fa fa-sticky-note-o
        AutomaticRekeyingAsNeeded = 4, // fa fa-rotate-left
        AutomaticRekeyingScheduled = 8, // fa fa-clock-o
        ExternalSignal = 16 // fa fa-flag
    }

    public class ManagedSecret : IDataStoreCompatibleStructure
    {
        public const int DEFAULT_NONCE_LENGTH = 64;

        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public TaskConfirmationStrategies TaskConfirmationStrategies { get; set; }

        public DateTime LastChanged { get; set; }
        public TimeSpan ValidPeriod { get; set; }

        public string Nonce { get; set; }

        public List<Guid> ResourceIds { get; set; } = new List<Guid>();

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
    }
}
