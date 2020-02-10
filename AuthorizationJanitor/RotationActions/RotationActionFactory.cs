using System;

namespace AuthorizationJanitor.RotationActions
{
    public static class RotationActionFactory
    {
        public static IRotation CreateRotationStrategy(JanitorConfigurationEntity.KeyType keyType)
        {
            switch (keyType)
            {
                case JanitorConfigurationEntity.KeyType.AccessToken:
                    return new RotationActions.AccessTokenRotation();
                case JanitorConfigurationEntity.KeyType.AzureStorageKey1:
                case JanitorConfigurationEntity.KeyType.AzureStorageKey2:
                case JanitorConfigurationEntity.KeyType.AzureStorageKerb1:
                case JanitorConfigurationEntity.KeyType.AzureStorageKerb2:
                    return new RotationActions.StorageKeyRotation();
                case JanitorConfigurationEntity.KeyType.CosmosDbPrimary:
                case JanitorConfigurationEntity.KeyType.CosmosDbPrimaryReadonly:
                case JanitorConfigurationEntity.KeyType.CosmosDbSecondary:
                case JanitorConfigurationEntity.KeyType.CosmosDbSecondaryReadonly:
                    return new RotationActions.CosmosDbKeyRotation();
                case JanitorConfigurationEntity.KeyType.EncryptionKey:
                    return new RotationActions.EncryptionKeyRotation();
                case JanitorConfigurationEntity.KeyType.ServiceBusPrimary:
                case JanitorConfigurationEntity.KeyType.ServiceBusSecondary:
                    return new RotationActions.ServiceBusKeyRotation();
                case JanitorConfigurationEntity.KeyType.SecretPassword:
                    return new RotationActions.PasswordRotation();
            }
            throw new NotImplementedException($"KeyType '{keyType}' not implemented");
        }
    }
}
