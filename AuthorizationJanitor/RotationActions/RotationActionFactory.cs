using System;

namespace AuthorizationJanitor.RotationActions
{
    public static class RotationActionFactory
    {
        public static IRotation CreateRotationStrategy(JanitorConfigurationEntity.AppSecretType appSecretType)
        {
            switch (appSecretType)
            {
                case JanitorConfigurationEntity.AppSecretType.AccessToken:
                    return new RotationActions.AccessTokenRotation();
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKey1:
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKey2:
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKerb1:
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKerb2:
                    return new RotationActions.StorageKeyRotation();
                case JanitorConfigurationEntity.AppSecretType.CosmosDbPrimary:
                case JanitorConfigurationEntity.AppSecretType.CosmosDbPrimaryReadonly:
                case JanitorConfigurationEntity.AppSecretType.CosmosDbSecondary:
                case JanitorConfigurationEntity.AppSecretType.CosmosDbSecondaryReadonly:
                    return new RotationActions.CosmosDbKeyRotation();
                case JanitorConfigurationEntity.AppSecretType.EncryptionKey:
                    return new RotationActions.EncryptionKeyRotation();
                case JanitorConfigurationEntity.AppSecretType.ServiceBusPrimary:
                case JanitorConfigurationEntity.AppSecretType.ServiceBusSecondary:
                    return new RotationActions.ServiceBusKeyRotation();
                case JanitorConfigurationEntity.AppSecretType.SecretPassword:
                    return new RotationActions.PasswordRotation();
            }
            throw new NotImplementedException($"AppSecret Type '{appSecretType}' not implemented");
        }
    }
}
