using Microsoft.Extensions.Logging;
using System;

namespace AuthorizationJanitor.Extensions
{
    /// <summary>
    /// Describes a ConsumingApplication which implements a slot pattern (such as Functions or WebApps)
    /// </summary>
    public abstract class SlottableConsumingApplicationExtension<TConsumerConfiguration> : ConsumingApplicationExtension<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableExtensionConfiguration
    {
        /// <summary>
        /// Source Slot (original application)
        /// </summary>
        public string SourceSlotName => Configuration.SourceSlot;

        /// <summary>
        /// Temporary Slot (to coalesce new keys/configuration)
        /// </summary>
        public string TemporarySlotName => Configuration.TemporarySlot;

        /// <summary>
        /// Destination Slot (updated application). By default this is the same as the Source Slot.
        /// </summary>
        public string DestinationSlotName => Configuration.DestinationSlot;

        public override string GetDescription()
        {
            return $"+ Slotted Deployment (Original: '{SourceSlotName}') (Temporary: '{TemporarySlotName}') (Destination: '{DestinationSlotName}')" + Environment.NewLine + base.GetDescription();
        }

        public SlottableConsumingApplicationExtension(ILogger logger, IRekeyableServiceExtension rekeyableService, TConsumerConfiguration configuration) : base(logger, rekeyableService, configuration) { }
    }
}
