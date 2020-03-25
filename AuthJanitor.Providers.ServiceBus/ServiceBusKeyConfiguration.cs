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
    }
}
