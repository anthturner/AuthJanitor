using System;
using System.Collections.Generic;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class KeyVaultConfiguration : AuthorizationJanitorExtensionConfiguration
    {
        public const int DEFAULT_SECRET_LENGTH = 64;

        /// <summary>
        /// Key Vault name (xxxxx.vault.azure.net)
        /// </summary>
        public string VaultName { get; set; }

        /// <summary>
        /// Name of Key or Secret being operated upon
        /// </summary>
        public string KeyOrSecretName { get; set; }

        /// <summary>
        /// Duration of valid period for key or secret
        /// </summary>
        public TimeSpan ValidPeriod { get; set; }

        /// <summary>
        /// Length of secret to regenerate, if appropriate
        /// </summary>
        public int SecretLength { get; set; } = DEFAULT_SECRET_LENGTH;

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine +
$"Key Vault Name: {VaultName} - Object Name: {KeyOrSecretName} - Valid Period: {ValidPeriod.ToString()} - Secret Length: {SecretLength}";
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
                    Score = 0.8,
                    Risk = $"The specificed secret length is extremely short ({SecretLength} characters), making it easier to compromise through brute force attacks",
                    Recommendation = "Increase the length of the secret to over 32 characters; prefer 64 or up."
                });
            }
            else if (SecretLength < 32)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 0.4,
                    Risk = $"The specificed secret length is somewhat short ({SecretLength} characters), making it easier to compromise through brute force attacks",
                    Recommendation = "Increase the length of the secret to over 32 characters; prefer 64 or up."
                });
            }

            if (ValidPeriod == TimeSpan.MaxValue)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 0.8,
                    Risk = $"The specificed Valid Period is TimeSpan.MaxValue, which is effectively Infinity; it is dangerous to allow infinite periods of validity because it allows an object's prior version to be available after the object has been rotated",
                    Recommendation = "Specify a reasonable value for Valid Period"
                });
            }
            else if (ValidPeriod == TimeSpan.Zero)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 1.0,
                    Risk = $"The specificed Valid Period is zero, so this object will never be allowed to be used",
                    Recommendation = "Specify a reasonable value for Valid Period"
                });
            }

            return issues;
        }
    }
}
