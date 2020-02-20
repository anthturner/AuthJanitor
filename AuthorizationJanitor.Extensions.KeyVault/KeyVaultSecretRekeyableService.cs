using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class KeyVaultSecretRekeyableService : RekeyableServiceExtension<KeyVaultConfiguration>
    {
        public KeyVaultSecretRekeyableService(ILogger logger, KeyVaultConfiguration configuration) : base(logger, configuration) { }

        public override async Task<RegeneratedKey> Rekey()
        {
            // TODO: This doesn't use the other credential set, it tries to execute its own set of fallbacks!
            SecretClient client = new SecretClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"), new DefaultAzureCredential(true));
            Azure.Response<KeyVaultSecret> currentSecret = await client.GetSecretAsync(Configuration.KeyOrSecretName);

            // Create a new version of the Secret
            KeyVaultSecret newSecret = new KeyVaultSecret(Configuration.KeyOrSecretName, HelperMethods.GenerateCryptographicallySecureString(Configuration.SecretLength));

            // Copy in metadata from the old Secret if it existed
            if (currentSecret != null && currentSecret.Value != null)
            {
                newSecret.Properties.ContentType = currentSecret.Value.Properties.ContentType;
                foreach (System.Collections.Generic.KeyValuePair<string, string> tag in currentSecret.Value.Properties.Tags)
                {
                    newSecret.Properties.Tags.Add(tag.Key, tag.Value);
                }
            }

            newSecret.Properties.NotBefore = DateTimeOffset.Now;
            newSecret.Properties.ExpiresOn = DateTimeOffset.Now + Configuration.ValidPeriod;

            Azure.Response<KeyVaultSecret> secretResponse = await client.SetSecretAsync(newSecret);

            return new RegeneratedKey()
            {
                NewKey = secretResponse.Value.Value
            };
        }
    }
}
