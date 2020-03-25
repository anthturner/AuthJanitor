using System;
using System.ComponentModel;

namespace AuthJanitor.Providers.KeyVault
{
    public class KeyVaultKeyConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Key Vault name (xxxxx.vault.azure.net)
        /// </summary>
        [Description("Vault Name")]
        public string VaultName { get; set; }

        /// <summary>
        /// Name of Key or Secret being operated upon
        /// </summary>
        [Description("Key Name")]
        public string KeyName { get; set; }
    }
}
