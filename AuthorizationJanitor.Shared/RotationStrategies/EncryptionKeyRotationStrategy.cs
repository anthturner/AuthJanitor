using AuthorizationJanitor.Shared.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Regenerates a Key Vault key with the same parameters as the previous version. The new key's ID is committed to the AppSecrets Key Vault.
    /// </summary>
    public class EncryptionKeyRotationStrategy : RotationStrategy<EncryptionKeyRotationStrategy.Configuration>
    {
        /// <summary>
        /// Configuration options for EncryptionKeyRotationStrategy
        /// </summary>
        public class Configuration : IRotationConfiguration
        {
            /// <summary>
            /// Name of Key Vault containing key to be regenerated
            /// </summary>
            public string KeyVaultName { get; set; }

            /// <summary>
            /// Key name to be regenerated
            /// </summary>
            public string KeyName { get; set; }
        }

        /// <summary>
        /// Regenerates a Key Vault key with the same parameters as the previous version. The new key's ID is committed to the AppSecrets Key Vault.
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret Configuration</param>
        public EncryptionKeyRotationStrategy(ILogger logger, AppSecretConfiguration configuration) : base(logger, configuration) { }

        /// <summary>
        /// Cache the current CosmosDB Key (regenerated if forced).
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, regenerate the CosmosDB key immediately</param>
        /// <returns>New CosmosDB Key to commit to Key Vault</returns>
        public override Task<string> CreateInitialData(bool forceRegeneration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Regenerate a Key Vault key
        /// </summary>
        /// <returns>New key version's Key ID, to commit to Key Vault</returns>
        public override async Task<string> Rotate()
        {
            var client = new KeyClient(new Uri($"https://{RotationConfiguration.KeyVaultName}.vault.azure.net/"), new DefaultAzureCredential(false));
            var currentKey = await client.GetKeyAsync(RotationConfiguration.KeyName);

            var creationOptions = new CreateKeyOptions()
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.Now + AppSecretConfiguration.AppSecretValidPeriod,
                NotBefore = DateTimeOffset.Now
            };
            foreach (var op in currentKey.Value.KeyOperations)
                creationOptions.KeyOperations.Add(op);
            foreach (var tag in currentKey.Value.Properties.Tags)
                creationOptions.Tags.Add(tag.Key, tag.Value);

            var key = await client.CreateKeyAsync(RotationConfiguration.KeyName, currentKey.Value.KeyType, creationOptions);

            AppSecretConfiguration.LastChanged = DateTime.Now;
            return key.Value.Id.ToString();
        }

        /// <summary>
        /// Sanity check ability to regenerate the Key Vault key. *This is currently not implemented!*
        /// </summary>
        /// <returns><c>TRUE</c> if able to rotate</returns>
        public override Task<bool> SanityCheck() => Task.FromResult(true);
    }
}
