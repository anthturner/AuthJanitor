using System.ComponentModel;

namespace AuthJanitor.Providers.KeyVault
{
    public class KeyVaultSecretLifecycleConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Key Vault name (xxxxx.vault.azure.net)
        /// </summary>
        [Description("Vault Name")]
        public string VaultName { get; set; }

        /// <summary>
        /// Name Secret being operated upon
        /// </summary>
        [Description("Secret Name")]
        public string SecretName { get; set; }

        /// <summary>
        /// Commit the ConnectionString instead of the Key
        /// </summary>
        [Description("Commit Connection String instead of Key")]
        public bool UseConnectionString { get; set; }
    }
}
