using AuthorizationJanitor.Targets;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;

namespace AuthorizationJanitor
{
    public class JanitorConfigurationEntity
    {
        public enum AppSecretType : int
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
        /// Name of AppSecret to rotate -- identifies the configuration, otherwise arbitrary.
        /// </summary>
        public string AppSecretName { get; set; }

        /// <summary>
        /// Type of key
        /// </summary>
        public AppSecretType Type { get; set; }

        /// <summary>
        /// Arbitrary, cryptographically safe random value to point to the VERSION of this AppSecret, but is not related to the actual secret value.
        /// Used to check for AppSecret updates from the application.
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Name of Key Vault Secret storing this credential or password in the janitor-managed AppSecret Key Vault
        /// </summary>
        public string KeyVaultSecretName { get; set; }

        /// <summary>
        /// Description of the Target
        /// </summary>
        public string TargetString { get; set; }

        /// <summary>
        /// Configurable TimeSpan representing the time between AppSecret rotations
        /// </summary>
        public TimeSpan AppSecretValidPeriod { get; set; }

        /// <summary>
        /// Last time this AppSecret was updated
        /// </summary>
        public DateTimeOffset LastChanged { get; set; }

        /// <summary>
        /// If the AppSecret is valid
        /// </summary>
        [IgnoreProperty]
        public bool IsValid => LastChanged + AppSecretValidPeriod < DateTime.Now;

        /// <summary>
        /// Updated AppSecret to commit to Key Vault; this is not and cannot be serialized into the configuration table!
        /// </summary>
        [IgnoreProperty]
        public string UpdatedAppSecret { get; set; }

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
                AppSecretName = AppSecretName,
                Type = Type,
                Nonce = Nonce,
                KeyVaultSecretName = KeyVaultSecretName,
                TargetString = TargetString,
                AppSecretValidPeriod = AppSecretValidPeriod,
                LastChanged = LastChanged
            };
        }
    }
}
