using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.KeyVault
{
    public class KeyVaultKeyRekeyableObjectProvider : RekeyableObjectProvider<KeyVaultConfiguration>
    {
        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            // TODO: This doesn't use the other credential set, it tries to execute its own set of fallbacks!
            KeyClient client = new KeyClient(new Uri($"https://{Configuration.VaultName}.vault.azure.net/"), new DefaultAzureCredential(true));
            Azure.Response<KeyVaultKey> currentKey = await client.GetKeyAsync(Configuration.KeyOrSecretName);

            CreateKeyOptions creationOptions = new CreateKeyOptions()
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.Now + requestedValidPeriod,
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
                    Score = 0.8,
                    Risk = $"The specificed Valid Period is TimeSpan.MaxValue, which is effectively Infinity; it is dangerous to allow infinite periods of validity because it allows an object's prior version to be available after the object has been rotated",
                    Recommendation = "Specify a reasonable value for Valid Period"
                });
            }
            else if (requestedValidPeriod == TimeSpan.Zero)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 1.0,
                    Risk = $"The specificed Valid Period is zero, so this object will never be allowed to be used",
                    Recommendation = "Specify a reasonable value for Valid Period"
                });
            }
            return issues.Union(base.GetRisks(requestedValidPeriod)).ToList();
        }
    }
}
