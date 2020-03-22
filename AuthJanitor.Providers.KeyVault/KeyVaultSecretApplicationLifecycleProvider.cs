using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.KeyVault
{
    [Provider(Name = "Key Vault Secret",
              IconClass = "fa fa-low-vision",
              Description = "Manages the lifecycle of a Key Vault Secret where a Managed Secret's value is stored")]
    [ProviderImage(ProviderImages.KEY_VAULT_SVG)]
    public class KeyVaultSecretApplicationLifecycleProvider : ApplicationLifecycleProvider<KeyVaultSecretConfiguration>
    {
        public KeyVaultSecretApplicationLifecycleProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
        {
        }

        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            // Connect to the Key Vault storing application secrets
            SecretClient client = new SecretClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"),
                _serviceProvider
                    .GetService<MultiCredentialProvider>()
                    .Get(MultiCredentialProvider.CredentialType.AgentServicePrincipal)
                    .DefaultAzureCredential);

            foreach (RegeneratedSecret secret in newSecrets)
            {
                Azure.Response<KeyVaultSecret> currentSecret = await client.GetSecretAsync(Configuration.SecretName);

                // Create a new version of the Secret
                string secretName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.SecretName : $"{Configuration.SecretName}-{secret.UserHint}";
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

                newKvSecret.Properties.NotBefore = DateTimeOffset.UtcNow;
                newKvSecret.Properties.ExpiresOn = secret.Expiry;

                await client.SetSecretAsync(newKvSecret);
            }
        }
    }
}
