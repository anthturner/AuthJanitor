using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuthJanitor.Providers.CosmosDb
{
    public class CosmosDbKeyConfiguration : AuthJanitorProviderConfiguration
    {
        public enum CosmosDbKeyKinds
        {
            Primary,
            Secondary,
            PrimaryReadOnly,
            SecondaryReadOnly
        }

        /// <summary>
        /// Kind (type) of CosmosDb Key
        /// </summary>
        [Description("CosmosDB Key Kind")]
        public CosmosDbKeyKinds KeyKind { get; set; }

        /// <summary>
        /// Skip the process of scrambling the other (non-active) key
        /// </summary>
        [Description("Skip Scrambling Other Key")]
        public bool SkipScramblingOtherKey { get; set; }
    }
}
