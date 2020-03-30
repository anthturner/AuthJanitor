﻿using Azure.Security.KeyVault.Secrets;
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
    public class KeyVaultSecretApplicationLifecycleProvider : ApplicationLifecycleProvider<KeyVaultSecretLifecycleConfiguration>
    {
        public KeyVaultSecretApplicationLifecycleProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            Logger.LogInformation("Committing new secrets to Key Vault secret {0}", Configuration.SecretName);
            var client = GetSecretClient();
            foreach (RegeneratedSecret secret in newSecrets)
            {
                Logger.LogInformation("Getting current secret version from secret name {0}", Configuration.SecretName);
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

                Logger.LogInformation("Committing new secret '{0}'", secretName);
                await client.SetSecretAsync(newKvSecret);
                Logger.LogInformation("Successfully committed new secret '{0}'", secretName);
            }
        }

        private SecretClient GetSecretClient() =>
            new SecretClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"),
                _serviceProvider
                    .GetService<MultiCredentialProvider>()
                    .Get(CredentialType)
                    .AzureIdentityTokenCredential);

        public override string GetDescription() =>
            $"Populates a Key Vault Secret called '{Configuration.SecretName}' " +
            $"from vault '{Configuration.VaultName}' with a given " +
            (Configuration.CommitAsConnectionString ? "connection string" : "key") + ".";
    }
}
