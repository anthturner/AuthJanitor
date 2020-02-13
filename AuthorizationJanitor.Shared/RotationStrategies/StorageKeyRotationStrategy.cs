using AuthorizationJanitor.Shared.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Regenerates Storage key. The new key is committed to the AppSecrets Key Vault.
    /// </summary>
    public class StorageKeyRotationStrategy : RotationStrategy<StorageKeyRotationStrategy.Configuration>
    {
        /// <summary>
        /// Configuration options for StorageKeyRotationStrategy
        /// </summary>
        public class Configuration : IRotationConfiguration
        {
            public enum StorageKeys
            {
                [Description("key1")]
                Key1,
                [Description("key2")]
                Key2,
                [Description("kerb1")]
                Kerb1,
                [Description("kerb2")]
                Kerb2,
            };

            /// <summary>
            /// Name of Azure Resource Group containing the CosmosDB
            /// </summary>
            public string ResourceGroupName { get; set; }

            /// <summary>
            /// Name of the CosmosDB Azure Resource in Resource Group
            /// </summary>
            public string ResourceName { get; set; }

            /// <summary>
            /// Authorization Rule to modify
            /// </summary>
            public string AuthorizationRule { get; set; }

            /// <summary>
            /// Storage Key type to rotate
            /// </summary>
            public StorageKeys KeyType { get; set; }
        }

        /// <summary>
        /// Regenerates Storage key. The new key is committed to the AppSecrets Key Vault.
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret Configuration</param>
        public StorageKeyRotationStrategy(ILogger logger, AppSecretConfiguration configuration) : base(logger, configuration) { }

        /// <summary>
        /// Cache the current Storage key (regenerate if forced).
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, regenerate the Storage key immediately</param>
        /// <returns>New Storage key to commit to Key Vault</returns>
        public override async Task<string> CreateInitialData(bool forceRegeneration)
        {
            if (forceRegeneration)
            {
                Logger.LogInformation("Force Regeneration is enabled; rotating key immediately.");
                return await Rotate();
            }
            else
            {
                Logger.LogInformation("Force Regeneration is disabled; Storage key will be cached but not rotated.");
                var azure = await HelperMethods.GetAzure();
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
                var rule = await storageAccount.GetKeysAsync();
                AppSecretConfiguration.LastChanged = DateTime.Now;
                return rule.FirstOrDefault(r => r.KeyName == RotationConfiguration.KeyType.GetEnumString())?.Value;
            }
        }

        /// <summary>
        /// Regenerate the Storage key
        /// </summary>
        /// <returns>New Storage key to commit to Key Vault</returns>
        public override async Task<string> Rotate()
        {
            var azure = await HelperMethods.GetAzure();
            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
            var newKeys = await storageAccount.RegenerateKeyAsync(RotationConfiguration.KeyType.GetEnumString());

            var newKey = newKeys.FirstOrDefault(k => k.KeyName == RotationConfiguration.KeyType.GetEnumString());
            AppSecretConfiguration.LastChanged = DateTime.Now;
            return newKey.Value;
        }

        /// <summary>
        /// Sanity check ability to reset the Storage Key. (by checking if it can read the key)
        /// </summary>
        /// <returns><c>TRUE</c> if able to rotate key</returns>
        public override async Task<bool> SanityCheck()
        {
            var azure = await HelperMethods.GetAzure();
            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
            var newKeys = await storageAccount.GetKeysAsync();
            return newKeys.Any(k => k.KeyName == RotationConfiguration.KeyType.GetEnumString() &&
                                    !string.IsNullOrEmpty(k.Value));
        }
    }
}
