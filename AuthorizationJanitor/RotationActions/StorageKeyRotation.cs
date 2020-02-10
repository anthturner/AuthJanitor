using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
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
            newEntity.UpdatedKey = newKey.Value;

            return newEntity;
        }

        private static string GetKeyName(JanitorConfigurationEntity.KeyType type)
        {
            switch (type)
            {
                case JanitorConfigurationEntity.KeyType.AzureStorageKey1: return "key1";
                case JanitorConfigurationEntity.KeyType.AzureStorageKey2: return "key2";
                case JanitorConfigurationEntity.KeyType.AzureStorageKerb1: return "kerb1";
                case JanitorConfigurationEntity.KeyType.AzureStorageKerb2: return "kerb2";
            }
            return null;
        }
    }
}
