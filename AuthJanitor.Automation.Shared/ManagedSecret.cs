using AuthJanitor.Providers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    [Serializable]
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

    public static class TaskConfirmationStrategiesExtensions
    {
        public static bool UsesOBOTokens(this TaskConfirmationStrategies confirmationStrategies) =>
            confirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminCachesSignOff) ||
            confirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminSignsOffJustInTime);
        public static bool UsesServicePrincipal(this TaskConfirmationStrategies confirmationStrategies) =>
            confirmationStrategies.HasFlag(TaskConfirmationStrategies.AutomaticRekeyingAsNeeded) ||
            confirmationStrategies.HasFlag(TaskConfirmationStrategies.AutomaticRekeyingScheduled) ||
            confirmationStrategies.HasFlag(TaskConfirmationStrategies.ExternalSignal);
    }

    public class ManagedSecret : IDataStoreCompatibleStructure
    {
        public const int DEFAULT_NONCE_LENGTH = 64;

        public Guid ObjectId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }

        public TaskConfirmationStrategies TaskConfirmationStrategies { get; set; } = TaskConfirmationStrategies.None;

        public DateTimeOffset LastChanged { get; set; } = DateTimeOffset.MinValue;
        public TimeSpan ValidPeriod { get; set; }

        public string Nonce { get; set; } = HelperMethods.GenerateCryptographicallySecureString(DEFAULT_NONCE_LENGTH);

        public IEnumerable<Guid> ResourceIds { get; set; } = new List<Guid>();

        /// <summary>
        /// If the ManagedSecret is valid
        /// </summary>
        [JsonIgnore]
        public bool IsValid => LastChanged + ValidPeriod < DateTime.Now;

        /// <summary>
        /// Date/Time of expiry
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset Expiry => LastChanged + ValidPeriod;

        /// <summary>
        /// Time remaining until Expiry (if expired, TimeSpan.Zero)
        /// </summary>
        [JsonIgnore]
        public TimeSpan TimeRemaining => IsValid ? Expiry - DateTimeOffset.Now : TimeSpan.Zero;
    }
}
