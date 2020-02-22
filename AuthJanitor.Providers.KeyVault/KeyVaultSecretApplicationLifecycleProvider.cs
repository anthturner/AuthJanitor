using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.KeyVault
{
    public class KeyVaultSecretApplicationLifecycleProvider : ApplicationLifecycleProvider<KeyVaultConfiguration>
    {
        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            // Connect to the Key Vault storing application secrets
            SecretClient client = new SecretClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"), new DefaultAzureCredential(true));

            foreach (RegeneratedSecret secret in newSecrets)
            {
                Azure.Response<KeyVaultSecret> currentSecret = await client.GetSecretAsync(Configuration.KeyOrSecretName);

                // Create a new version of the Secret
                string secretName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.KeyOrSecretName : $"{Configuration.KeyOrSecretName}-{secret.UserHint}";
                KeyVaultSecret newKvSecret = new KeyVaultSecret(secretName, secret.NewSecretValue);

                // Copy in metadata from the old Secret if it existed
                if (currentSecret != null && currentSecret.Value != null)
                {
                    newKvSecret.Properties.ContentType = currentSecret.Value.Properties.ContentType;
                    foreach (System.Collections.Generic.KeyValuePair<string, string> tag in currentSecret.Value.Properties.Tags)
                    {
                        newKvSecret.Properties.Tags.Add(tag.Key, tag.Value);
                    }
                    newKvSecret.Properties.Tags.Add("UserHint", secret.UserHint);
                }

                newKvSecret.Properties.NotBefore = DateTimeOffset.Now;
                newKvSecret.Properties.ExpiresOn = secret.Expiry;

                await client.SetSecretAsync(newKvSecret);
            }
        }
    }
}
