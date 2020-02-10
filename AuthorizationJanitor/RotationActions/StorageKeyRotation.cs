using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    /// <summary>
    /// Regenerates an Azure Storage key and commits it to the AppSecrets Key Vault
    /// </summary>
    public class StorageKeyRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var target = newEntity.GetTarget<Targets.NamedResourceTarget>();

            var azure = await HelperMethods.GetAzure();
            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(target.ResourceGroup, target.ResourceName);
            var newKeys = await storageAccount.RegenerateKeyAsync(GetKeyName(entity.Type));

            var newKey = newKeys.FirstOrDefault(k => k.KeyName == GetKeyName(entity.Type));
            newEntity.LastChanged = DateTime.Now;
            newEntity.UpdatedAppSecret = newKey.Value;

            return newEntity;
        }

        private static string GetKeyName(JanitorConfigurationEntity.AppSecretType type)
        {
            switch (type)
            {
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKey1: return "key1";
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKey2: return "key2";
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKerb1: return "kerb1";
                case JanitorConfigurationEntity.AppSecretType.AzureStorageKerb2: return "kerb2";
            }
            return null;
        }
    }
}
