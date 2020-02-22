using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuthJanitor.Providers.Storage
{
    public class StorageAccountKeyConfiguration : AuthJanitorProviderConfiguration
    {
        public enum StorageKeyTypes
        {
            Key1,
            Key2,
            Kerb1,
            Kerb2
        }

        /// <summary>
        /// Kind (type) of Storage Key
        /// </summary>
        [Description("Storage Key")]
        public StorageKeyTypes KeyType { get; set; }

        /// <summary>
        /// Skip the process of scrambling the other (non-active) key
        /// </summary>
        [Description("Skip Scrambling Other Key?")]
        public bool SkipScramblingOtherKey { get; set; }

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine +
$"Storage Account Key Kind: {KeyType.ToString()} - Skipping Scramble of Other Key? {SkipScramblingOtherKey.ToString()}";
        }

        /// <summary>
        /// Get a list of configuration choices that might be risky
        /// </summary>
        /// <returns></returns>
        public override IList<RiskyConfigurationItem> GetRiskyConfigurations()
        {
            List<RiskyConfigurationItem> issues = new List<RiskyConfigurationItem>();
            if (SkipScramblingOtherKey)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 0.8,
                    Risk = $"The other (unused) Storage Account Key of this type is not being scrambled during key rotation",
                    Recommendation = "Unless other services use the alternate key, consider allowing the scrambling of the unused key to 'fully' rekey the Storage Account and maintain a high degree of security."
                });
            }

            return issues;
        }
    }
}
