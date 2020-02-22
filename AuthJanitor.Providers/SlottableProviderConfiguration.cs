using System;
using System.ComponentModel;

namespace AuthJanitor.Providers
{
    /// <summary>
    /// Describes the configuration of an Extension which supports slotting
    /// </summary>
    public abstract class SlottableProviderConfiguration : AuthJanitorProviderConfiguration
    {
        public const string DEFAULT_ORIGINAL_SLOT = "production";
        public const string DEFAULT_TEMPORARY_SLOT = "aj-temporary";
        public const string DEFAULT_DESTINATION_SLOT = DEFAULT_ORIGINAL_SLOT;

        /// <summary>
        /// Source Slot (original application)
        /// </summary>
        [Description("Original Application Slot")]
        public string SourceSlot { get; set; } = DEFAULT_ORIGINAL_SLOT;

        /// <summary>
        /// Temporary Slot (to coalesce new keys/configuration)
        /// </summary>
        [Description("Temporary Application Slot")]
        public string TemporarySlot { get; set; } = DEFAULT_TEMPORARY_SLOT;

        /// <summary>
        /// Destination Slot (updated application). By default this is the same as the Source Slot.
        /// </summary>
        [Description("Destination Application Slot")]
        public string DestinationSlot { get; set; } = DEFAULT_DESTINATION_SLOT;

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine +
                $"Slots = Source: {SourceSlot} - Temporary: {TemporarySlot} - Destination: {DestinationSlot}";
        }
    }
}
