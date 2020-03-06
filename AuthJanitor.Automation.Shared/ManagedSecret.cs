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

        public Guid ObjectId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }

        public TaskConfirmationStrategies TaskConfirmationStrategies { get; set; }

        public DateTimeOffset LastChanged { get; set; } = DateTimeOffset.MinValue;
        public TimeSpan ValidPeriod { get; set; }

        public string Nonce { get; set; } = HelperMethods.GenerateCryptographicallySecureString(DEFAULT_NONCE_LENGTH);

        public List<Guid> ResourceIds { get; set; } = new List<Guid>();

        /// <summary>
        /// If the ManagedSecret is valid
        /// </summary>
        [JsonIgnore]
        public bool IsValid => LastChanged + ValidPeriod < DateTime.Now;
    }
}
