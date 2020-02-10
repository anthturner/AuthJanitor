using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    /// <summary>
    /// Regenerates a requested Key Vault Key and commits the new KID to the AppSecrets Key Vault
    /// </summary>
    public class EncryptionKeyRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var target = newEntity.GetTarget<Targets.KeyVaultTarget>();

            var client = new KeyClient(new Uri($"https://{target.VaultName}.vault.azure.net/"), new DefaultAzureCredential(false));
            var currentKey = await client.GetKeyAsync(target.KeyOrSecretName);

            var creationOptions = new CreateKeyOptions()
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.Now + entity.AppSecretValidPeriod,
                NotBefore = DateTimeOffset.Now
            };
            foreach (var op in currentKey.Value.KeyOperations)
                creationOptions.KeyOperations.Add(op);
            foreach (var tag in currentKey.Value.Properties.Tags)
                creationOptions.Tags.Add(tag.Key, tag.Value);

            var key = await client.CreateKeyAsync(target.KeyOrSecretName, currentKey.Value.KeyType, creationOptions);

            newEntity.UpdatedAppSecret = key.Value.Id.ToString();
            newEntity.LastChanged = DateTime.Now;

            return newEntity;
        }
    }
}
