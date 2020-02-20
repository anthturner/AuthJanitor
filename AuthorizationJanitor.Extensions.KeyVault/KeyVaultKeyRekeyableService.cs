using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class KeyVaultKeyRekeyableService : RekeyableServiceExtension<KeyVaultConfiguration>
    {
        public KeyVaultKeyRekeyableService(ILogger logger, KeyVaultConfiguration configuration) : base(logger, configuration) { }

        public override async Task<RegeneratedKey> Rekey()
        {
            // TODO: This doesn't use the other credential set, it tries to execute its own set of fallbacks!
            KeyClient client = new KeyClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"), new DefaultAzureCredential(true));
            Azure.Response<KeyVaultKey> currentKey = await client.GetKeyAsync(Configuration.KeyOrSecretName);

            CreateKeyOptions creationOptions = new CreateKeyOptions()
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.Now + Configuration.ValidPeriod,
                NotBefore = DateTimeOffset.Now
            };
            foreach (KeyOperation op in currentKey.Value.KeyOperations)
            {
                creationOptions.KeyOperations.Add(op);
            }

            foreach (System.Collections.Generic.KeyValuePair<string, string> tag in currentKey.Value.Properties.Tags)
            {
                creationOptions.Tags.Add(tag.Key, tag.Value);
            }

            Azure.Response<KeyVaultKey> key = await client.CreateKeyAsync(Configuration.KeyOrSecretName, currentKey.Value.KeyType, creationOptions);

            return new RegeneratedKey() { NewKey = key.Value.Key.Id.ToString() };
        }
    }
}
