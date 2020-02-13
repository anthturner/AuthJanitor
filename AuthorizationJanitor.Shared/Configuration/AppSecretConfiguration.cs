using AuthorizationJanitor.Shared.RotationStrategies;
using Newtonsoft.Json;
using System;

namespace AuthorizationJanitor.Shared.Configuration
{
    public class AppSecretConfiguration
    {
        public enum AppSecretType : int
        {
            AccessToken = 1,
            EncryptionKey = 2,
            AzureStorage = 3,
            CosmosDb = 4,
            ServiceBus = 5,
            SecretPassword = 6
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
        /// JSON Serialized Rotation Configuration
        /// </summary>
        public string SerializedRotationConfiguration { get; set; }

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
        [JsonIgnore]
        public bool IsValid => LastChanged + AppSecretValidPeriod < DateTime.Now;

        /// <summary>
        /// Updated AppSecret to commit to Key Vault; this is not and cannot be serialized into the configuration table!
        /// </summary>
        [JsonIgnore]
        public string UpdatedAppSecret { get; set; }

        /// <summary>
        /// Get the RotationConfiguration based on its expected Type
        /// </summary>
        /// <typeparam name="TRotationConfiguration">Rotation configuration Type</typeparam>
        /// <returns>RotationConfiguration object for type</returns>
        public TRotationConfiguration GetRotationConfiguration<TRotationConfiguration>() where TRotationConfiguration : IRotationConfiguration => 
            JsonConvert.DeserializeObject<TRotationConfiguration>(SerializedRotationConfiguration);

        /// <summary>
        /// Update the RotationConfiguration
        /// </summary>
        /// <typeparam name="TRotationConfiguration">Rotation configuration Type</typeparam>
        /// <param name="configuration">Updated configuration</param>
        public void PutRotationConfiguration<TRotationConfiguration>(TRotationConfiguration configuration) where TRotationConfiguration : IRotationConfiguration =>
            SerializedRotationConfiguration = JsonConvert.SerializeObject(configuration);
    }
}
