using AuthorizationJanitor.Targets;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;

namespace AuthorizationJanitor
{
    public class JanitorConfigurationEntity
    {
        public enum KeyType : int
        {
            AccessToken = 1,
            EncryptionKey = 2,
            AzureStorageKey1 = 3,
            AzureStorageKey2 = 4,
            AzureStorageKerb1 = 5,
            AzureStorageKerb2 = 6,
            CosmosDbPrimary = 7,
            CosmosDbPrimaryReadonly = 8,
            CosmosDbSecondary = 9,
            CosmosDbSecondaryReadonly = 10,
            ServiceBusPrimary = 11,
            ServiceBusSecondary = 12,
            SecretPassword = 13
        }

        /// <summary>
        /// Name of key to rotate -- identifies the key configuration, otherwise arbitrary.
        /// </summary>
        public string FriendlyKeyName { get; set; }

        /// <summary>
        /// Type of key
        /// </summary>
        public KeyType Type { get; set; }

        /// <summary>
        /// Arbitrary, cryptographically safe random value to point to the VERSION of this key, but is not related to the actual key value.
        /// Used to check for key updates from the application.
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Name of Secret storing this credential or password in the janitor-managed Key Vault
        /// </summary>
        public string KeyVaultSecretName { get; set; }

        /// <summary>
        /// Description of the Target
        /// </summary>
        public string TargetString { get; set; }

        /// <summary>
        /// Configurable timespan representing the time between key rotations
        /// </summary>
        public TimeSpan KeyValidPeriod { get; set; }

        /// <summary>
        /// Last time this key was updated
        /// </summary>
        public DateTime LastChanged { get; set; }

        /// <summary>
        /// If the key is valid
        /// </summary>
        [IgnoreProperty]
        public bool IsValid => LastChanged + KeyValidPeriod < DateTime.Now;

        /// <summary>
        /// Updated key to commit to Key Vault; this is not and cannot be serialized into the configuration table!
        /// </summary>
        [IgnoreProperty]
        public string UpdatedKey { get; set; }

        /// <summary>
        /// Get the Target details
        /// </summary>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <returns></returns>
        public TTarget GetTarget<TTarget>() where TTarget : ITarget => JsonConvert.DeserializeObject<TTarget>(TargetString);

        public JanitorConfigurationEntity Clone()
        {
            return new JanitorConfigurationEntity()
            {
                FriendlyKeyName = FriendlyKeyName,
                Type = Type,
                Nonce = Nonce,
                KeyVaultSecretName = KeyVaultSecretName,
                TargetString = TargetString,
                KeyValidPeriod = KeyValidPeriod,
                LastChanged = LastChanged
            };
        }
    }
}
