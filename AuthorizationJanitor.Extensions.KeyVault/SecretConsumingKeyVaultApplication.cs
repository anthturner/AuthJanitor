using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class SecretConsumingKeyVaultApplication : ConsumingApplicationExtension<KeyVaultConfiguration>
    {
        public SecretConsumingKeyVaultApplication(ILogger logger,
            IRekeyableServiceExtension rekeyableService,
            KeyVaultConfiguration configuration) : base(logger, rekeyableService, configuration) { }

        public override async Task Rekey()
        {
            // No pre-work, just go straight to rekeying the Service
            RegeneratedKey newKey = await Service.Rekey();

            // Connect to the Key Vault storing application secrets
            SecretClient client = new SecretClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"), new DefaultAzureCredential(true));
            Azure.Response<KeyVaultSecret> currentSecret = await client.GetSecretAsync(Configuration.KeyOrSecretName);

            // Create a new version of the Secret
            KeyVaultSecret newSecret = new KeyVaultSecret(Configuration.KeyOrSecretName, newKey.NewKey);

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
            // TODO: Set expiry

            await client.SetSecretAsync(newSecret);

            // Signal the service that the ConsumingApplication has been swapped to the new key
            await Service.OnConsumingApplicationSwapped();
        }
    }
}
