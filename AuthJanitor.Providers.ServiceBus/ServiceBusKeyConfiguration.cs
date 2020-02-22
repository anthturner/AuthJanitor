using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuthJanitor.Providers.ServiceBus
{
    public class ServiceBusKeyConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Duplication of Service Bus "Policykey" enumeration to avoid passing through a dependency
        /// </summary>
        public enum ServiceBusKeyTypes
        {
            Primary,
            Secondary
        }

        /// <summary>
        /// Kind (type) of Service Bus Key
        /// </summary>
        [Description("Service Bus Key Type")]
        public ServiceBusKeyTypes KeyType { get; set; }

        /// <summary>
        /// Service Bus Authorization Rule name
        /// </summary>
        [Description("Authorization Rule")]
        public string AuthorizationRuleName { get; set; }

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
$"Service Bus Key Type: {KeyType.ToString()} - Authorization Rule: {AuthorizationRuleName} - Skipping Scramble of Other Key? {SkipScramblingOtherKey.ToString()}";
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
                    Risk = $"The other (unused) Service Bus Key is not being scrambled during key rotation",
                    Recommendation = "Unless other services use the alternate key, consider allowing the scrambling of the unused key to 'fully' rekey the Service Bus and maintain a high degree of security."
                });
            }

            return issues;
        }
    }
}
