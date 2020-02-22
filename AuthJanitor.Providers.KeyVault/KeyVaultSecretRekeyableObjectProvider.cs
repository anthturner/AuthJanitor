using AuthJanitor.Providers;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.KeyVault
{
    public class KeyVaultSecretRekeyableObjectProvider : RekeyableObjectProvider<KeyVaultConfiguration>
    {
        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
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
                foreach (KeyValuePair<string, string> tag in currentSecret.Value.Properties.Tags)
                {
                    newSecret.Properties.Tags.Add(tag.Key, tag.Value);
                }
            }

            newSecret.Properties.NotBefore = DateTimeOffset.Now;
            newSecret.Properties.ExpiresOn = DateTimeOffset.Now + requestedValidPeriod;

            Azure.Response<KeyVaultSecret> secretResponse = await client.SetSecretAsync(newSecret);

            return new RegeneratedSecret()
            {
                Expiry = newSecret.Properties.ExpiresOn.Value,
                UserHint = Configuration.UserHint,
                NewSecretValue = secretResponse.Value.Value
            };
        }

        public override IList<RiskyConfigurationItem> GetRisks(TimeSpan requestedValidPeriod)
        {
            var issues = new List<RiskyConfigurationItem>();
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
