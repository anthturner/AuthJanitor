using AuthorizationJanitor.Shared.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Creates RotationStrategy instances
    /// </summary>
    public static class RotationStrategyFactory
    {
        /// <summary>
        /// Creates instance of IRotationStrategy from the AppSecret configuration's type
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret configuration</param>
        /// <returns>RotationStrategy instance from given configuration's type</returns>
        public static IRotationStrategy CreateRotationStrategy(ILogger logger, AppSecretConfiguration configuration) =>
            configuration.Type switch
            {
                AppSecretConfiguration.AppSecretType.AccessToken => new AccessTokenRotationStrategy(logger, configuration),
                AppSecretConfiguration.AppSecretType.AzureStorage => new StorageKeyRotationStrategy(logger, configuration),
                AppSecretConfiguration.AppSecretType.CosmosDb => new CosmosDbKeyRotationStrategy(logger, configuration),
                AppSecretConfiguration.AppSecretType.EncryptionKey => new EncryptionKeyRotationStrategy(logger, configuration),
                AppSecretConfiguration.AppSecretType.SecretPassword => new PasswordRotationStrategy(logger, configuration),
                AppSecretConfiguration.AppSecretType.ServiceBus => new ServiceBusKeyRotationStrategy(logger, configuration),
                _ => throw new NotImplementedException($"Rotation strategy '{configuration.Type.ToString()}' not supported.")
            };

        /// <summary>
        /// Creates instance of IRotationConfiguration from the AppSecret configuration's type
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IRotationConfiguration CreateRotationConfiguration(AppSecretConfiguration configuration) =>
            configuration.Type switch
            {
                AppSecretConfiguration.AppSecretType.AccessToken => new AccessTokenRotationStrategy.Configuration(),
                AppSecretConfiguration.AppSecretType.AzureStorage => new StorageKeyRotationStrategy.Configuration(),
                AppSecretConfiguration.AppSecretType.CosmosDb => new CosmosDbKeyRotationStrategy.Configuration(),
                AppSecretConfiguration.AppSecretType.EncryptionKey => new EncryptionKeyRotationStrategy.Configuration(),
                AppSecretConfiguration.AppSecretType.SecretPassword => new PasswordRotationStrategy.Configuration(),
                AppSecretConfiguration.AppSecretType.ServiceBus => new ServiceBusKeyRotationStrategy.Configuration(),
            };
    }
}
