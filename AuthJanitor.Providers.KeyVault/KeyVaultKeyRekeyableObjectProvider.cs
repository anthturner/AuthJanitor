﻿using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.KeyVault
{
    [Provider(Name = "Key Vault Key",
              IconClass = "fa fa-key",
              Description = "Regenerates an Azure Key Vault Key with the same parameters as the previous version")]
    [ProviderImage(ProviderImages.KEY_VAULT_SVG)]
    public class KeyVaultKeyRekeyableObjectProvider : RekeyableObjectProvider<KeyVaultKeyConfiguration>
    {
        public KeyVaultKeyRekeyableObjectProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public override async Task<RegeneratedSecret> GetSecretToUseDuringRekeying()
        {
            var client = GetKeyClient();
            Azure.Response<KeyVaultKey> currentKey = await client.GetKeyAsync(Configuration.KeyName);

            return new RegeneratedSecret()
            {
                UserHint = Configuration.UserHint,
                NewSecretValue = currentKey.Value.Key.Id.ToString()
            };
        }

        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            var client = GetKeyClient();
            Azure.Response<KeyVaultKey> currentKey = await client.GetKeyAsync(Configuration.KeyName);

            CreateKeyOptions creationOptions = new CreateKeyOptions()
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.UtcNow + requestedValidPeriod,
                NotBefore = DateTimeOffset.UtcNow
            };
            foreach (KeyOperation op in currentKey.Value.KeyOperations)
            {
                creationOptions.KeyOperations.Add(op);
            }

            foreach (System.Collections.Generic.KeyValuePair<string, string> tag in currentKey.Value.Properties.Tags)
            {
                creationOptions.Tags.Add(tag.Key, tag.Value);
            }

            Azure.Response<KeyVaultKey> key = await client.CreateKeyAsync(Configuration.KeyName, currentKey.Value.KeyType, creationOptions);

            return new RegeneratedSecret()
            {
                UserHint = Configuration.UserHint,
                NewSecretValue = key.Value.Key.Id.ToString()
            };
        }

        public override IList<RiskyConfigurationItem> GetRisks(TimeSpan requestedValidPeriod)
        {
            List<RiskyConfigurationItem> issues = new List<RiskyConfigurationItem>();
            if (requestedValidPeriod == TimeSpan.MaxValue)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 80,
                    Risk = $"The specificed Valid Period is TimeSpan.MaxValue, which is effectively Infinity; it is dangerous to allow infinite periods of validity because it allows an object's prior version to be available after the object has been rotated",
                    Recommendation = "Specify a reasonable value for Valid Period"
                });
            }
            else if (requestedValidPeriod == TimeSpan.Zero)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 100,
                    Risk = $"The specificed Valid Period is zero, so this object will never be allowed to be used",
                    Recommendation = "Specify a reasonable value for Valid Period"
                });
            }
            return issues.Union(base.GetRisks(requestedValidPeriod)).ToList();
        }

        public override string GetDescription() =>
            $"Regenerates the key called '{Configuration.KeyName}' from vault " +
            $"'{Configuration.VaultName}' with its current parameters and at its " +
            $"current key size.";

        private KeyClient GetKeyClient() =>
            new KeyClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"),
                _serviceProvider
                    .GetService<MultiCredentialProvider>()
                    .Get(CredentialType)
                    .AzureIdentityTokenCredential);
    }
}
