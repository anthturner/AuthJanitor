using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AuthJanitor.Providers.KeyVault
{
    public class KeyVaultConfiguration : AuthJanitorProviderConfiguration
    {
        public const int DEFAULT_SECRET_LENGTH = 64;

        /// <summary>
        /// Key Vault name (xxxxx.vault.azure.net)
        /// </summary>
        [Description("Vault Name")]
        public string VaultName { get; set; }

        /// <summary>
        /// Name of Key or Secret being operated upon
        /// </summary>
        [Description("Key or Secret Name")]
        public string KeyOrSecretName { get; set; }

        /// <summary>
        /// Length of secret to regenerate, if appropriate
        /// </summary>
        [Description("New Secret Length")]
        public int SecretLength { get; set; } = DEFAULT_SECRET_LENGTH;

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine +
$"Key Vault Name: {VaultName} - Object Name: {KeyOrSecretName} - Secret Length: {SecretLength}";
        }

        /// <summary>
        /// Get a list of configuration choices that might be risky
        /// </summary>
        /// <returns></returns>
        public override IList<RiskyConfigurationItem> GetRiskyConfigurations()
        {
            List<RiskyConfigurationItem> issues = new List<RiskyConfigurationItem>();
            if (SecretLength < 16)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 80,
                    Risk = $"The specificed secret length is extremely short ({SecretLength} characters), making it easier to compromise through brute force attacks",
                    Recommendation = "Increase the length of the secret to over 32 characters; prefer 64 or up."
                });
            }
            else if (SecretLength < 32)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 40,
                    Risk = $"The specificed secret length is somewhat short ({SecretLength} characters), making it easier to compromise through brute force attacks",
                    Recommendation = "Increase the length of the secret to over 32 characters; prefer 64 or up."
                });
            }

            return issues.Union(base.GetRiskyConfigurations()).ToList();
        }
    }
}
