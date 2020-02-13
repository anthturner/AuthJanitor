using AuthorizationJanitor.Shared.Configuration;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Regenerates CosmosDB keys. The new key is committed to the AppSecrets Key Vault.
    /// </summary>
    public class CosmosDbKeyRotationStrategy : RotationStrategy<CosmosDbKeyRotationStrategy.Configuration>
    {
        /// <summary>
        /// Configuration options for CosmosDbKeyRotationStrategy
        /// </summary>
        public class Configuration : IRotationConfiguration
        {
            public enum CosmosKeys
            {
                [Description("primary")]
                Primary,
                [Description("primaryReadonly")]
                PrimaryReadOnly,
                [Description("secondary")]
                Secondary,
                [Description("secondaryReadonly")]
                SecondaryReadOnly
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
            /// CosmosDB Key type to rotate
            /// </summary>
            public CosmosKeys KeyType { get; set; }
        }

        /// <summary>
        /// Regenerates CosmosDB keys. The new key is committed to the AppSecrets Key Vault.
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret Configuration</param>
        public CosmosDbKeyRotationStrategy(ILogger logger, AppSecretConfiguration configuration) : base(logger, configuration) { }

        /// <summary>
        /// Cache the current CosmosDB Key (regenerated if forced).
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, regenerate the CosmosDB key immediately</param>
        /// <returns>New CosmosDB Key to commit to Key Vault</returns>
        public override async Task<string> CreateInitialData(bool forceRegeneration)
        {
            if (forceRegeneration)
            {
                Logger.LogInformation("Force Regeneration is enabled; rotating key immediately.");
                return await Rotate();
            }
            else
            {
                Logger.LogInformation("Force Regeneration is disabled; CosmosDB keys will be cached but not rotated.");
                var azure = await HelperMethods.GetAzure();
                var cosmosDbAccount = await azure.CosmosDBAccounts.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
                AppSecretConfiguration.LastChanged = DateTime.Now;

                // Get new key value
                var keys = await cosmosDbAccount.ListKeysAsync();
                return GetKeyValue(keys);
            }
        }

        /// <summary>
        /// Regenerate the CosmosDB key
        /// </summary>
        /// <returns>New CosmosDB Key to commit to Key Vault</returns>
        public override async Task<string> Rotate()
        {
            var azure = await HelperMethods.GetAzure();
            var cosmosDbAccount = await azure.CosmosDBAccounts.GetByResourceGroupAsync(RotationConfiguration.ResourceGroupName, RotationConfiguration.ResourceName);
            await cosmosDbAccount.RegenerateKeyAsync(AppSecretConfiguration.Type.GetEnumString());
            AppSecretConfiguration.LastChanged = DateTime.Now;

            // Get new key value
            var keys = await cosmosDbAccount.ListKeysAsync();
            return GetKeyValue(keys);
        }

        /// <summary>
        /// Sanity check ability to reset the CosmosDB Keys. *This is currently not implemented!*
        /// </summary>
        /// <returns><c>TRUE</c> if able to rotate</returns>
        public override Task<bool> SanityCheck() => Task.FromResult(true);

        private string GetKeyValue(IDatabaseAccountListKeysResult keys)
        {
            return RotationConfiguration.KeyType switch
            {
                Configuration.CosmosKeys.Primary => keys.PrimaryMasterKey,
                Configuration.CosmosKeys.PrimaryReadOnly => keys.PrimaryReadonlyMasterKey,
                Configuration.CosmosKeys.Secondary => keys.SecondaryMasterKey,
                Configuration.CosmosKeys.SecondaryReadOnly => keys.SecondaryReadonlyMasterKey,
                _ => null,
            };
        }
    }
}
