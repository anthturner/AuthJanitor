using AuthorizationJanitor.Shared.Configuration;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Regenerates Service Bus key. The new key is committed to the AppSecrets Key Vault.
    /// </summary>
    public class ServiceBusKeyRotationStrategy : RotationStrategy<ServiceBusKeyRotationStrategy.Configuration>
    {
        /// <summary>
        /// Configuration options for ServiceBusKeyRotationStrategy
        /// </summary>
        public class Configuration : IRotationConfiguration
        {
            public enum ServiceBusKeys
            {
                [Description("primary")]
                Primary,
                [Description("secondary")]
                Secondary
            };

            /// <summary>
            /// Name of Azure Resource Group containing the Service Bus
            /// </summary>
            public string ResourceGroupName { get; set; }

            /// <summary>
            /// Name of the Service Bus Azure Resource in Resource Group
            /// </summary>
            public string ResourceName { get; set; }

            /// <summary>
            /// Authorization Rule to modify
            /// </summary>
            public string AuthorizationRule { get; set; }

            /// <summary>
            /// Service Bus Key type to rotate
            /// </summary>
            public ServiceBusKeys KeyType { get; set; }
        }

        /// <summary>
        /// Regenerates Service Bus key. The new key is committed to the AppSecrets Key Vault.
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret Configuration</param>
        public ServiceBusKeyRotationStrategy(ILogger logger, AppSecretConfiguration configuration) : base(logger, configuration) { }

        /// <summary>
        /// Cache the current Service Bus key (regenerate if forced).
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, regenerate the Service Bus key immediately</param>
        /// <returns>New Service Bus key to commit to Key Vault</returns>
        public override async Task<string> CreateInitialData(bool forceRegeneration)
        {
            if (forceRegeneration)
            {
                Logger.LogInformation("Force Regeneration is enabled; rotating key immediately.");
                return await Rotate();
            }
            else
            {
                Logger.LogInformation("Force Regeneration is disabled; Service Bus key will be cached but not rotated.");
                var azure = await HelperMethods.GetAzure();
                var serviceBusNamespace = await azure.ServiceBusNamespaces.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
                var rule = await serviceBusNamespace.AuthorizationRules.GetByNameAsync(RotationConfiguration.AuthorizationRule);

                AppSecretConfiguration.LastChanged = DateTime.Now;

                var keys = await rule.GetKeysAsync();
                return GetKeyValue(keys);
            }
        }

        /// <summary>
        /// Regenerate the Service Bus key
        /// </summary>
        /// <returns>New Service Bus key to commit to Key Vault</returns>
        public override async Task<string> Rotate()
        {
            var azure = await HelperMethods.GetAzure();
            var serviceBusNamespace = await azure.ServiceBusNamespaces.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
            var rule = await serviceBusNamespace.AuthorizationRules.GetByNameAsync(RotationConfiguration.AuthorizationRule);
            rule.Rights[0].HasFlag(AccessRights.Manage);
            var newKey = await rule.RegenerateKeyAsync(PolicyKey);
            AppSecretConfiguration.LastChanged = DateTime.Now;
            return GetKeyValue(newKey);
        }

        /// <summary>
        /// Sanity check ability to reset the Storage Keys.
        /// </summary>
        /// <returns><c>TRUE</c> if able to rotate keys</returns>
        public override async Task<bool> SanityCheck()
        {
            var azure = await HelperMethods.GetAzure();
            var serviceBusNamespace = await azure.ServiceBusNamespaces.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
            var rule = await serviceBusNamespace.AuthorizationRules.GetByNameAsync(RotationConfiguration.AuthorizationRule);
            return rule.Rights[0].HasFlag(AccessRights.Manage);
        }

        private Policykey PolicyKey =>
            RotationConfiguration.KeyType switch
            {
                Configuration.ServiceBusKeys.Primary => Policykey.PrimaryKey,
                Configuration.ServiceBusKeys.Secondary => Policykey.SecondaryKey,
                _ => throw new NotImplementedException($"Service Bus Key Type '{RotationConfiguration.KeyType}' unknown!"),
            };

        private string GetKeyValue(IAuthorizationKeys keys) =>
            RotationConfiguration.KeyType switch
            {
                Configuration.ServiceBusKeys.Primary => keys.PrimaryKey,
                Configuration.ServiceBusKeys.Secondary => keys.SecondaryKey,
                _ => throw new NotImplementedException($"Service Bus Key Type '{RotationConfiguration.KeyType}' unknown!"),
            };
    }
}
