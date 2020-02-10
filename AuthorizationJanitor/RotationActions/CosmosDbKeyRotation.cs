using Microsoft.Azure.Management.CosmosDB.Fluent;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    public class CosmosDbKeyRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var target = newEntity.GetTarget<Targets.NamedResourceTarget>();
            
            var azure = await HelperMethods.GetAzure();
            var cosmosDbAccount = await azure.CosmosDBAccounts.GetByResourceGroupAsync(target.ResourceGroup, target.ResourceName);
            await cosmosDbAccount.RegenerateKeyAsync(GetKeyName(entity.Type));
            
            // Get new key value
            var keys = await cosmosDbAccount.ListKeysAsync();
            newEntity.UpdatedKey = GetKeyValue(keys, entity.Type);
            newEntity.LastChanged = DateTime.Now;

            return newEntity;
        }

        private static string GetKeyName(JanitorConfigurationEntity.KeyType type)
        {
            switch (type)
            {
                case JanitorConfigurationEntity.KeyType.CosmosDbPrimary: return "primary";
                case JanitorConfigurationEntity.KeyType.CosmosDbPrimaryReadonly: return "primaryReadonly";
                case JanitorConfigurationEntity.KeyType.CosmosDbSecondary: return "secondary";
                case JanitorConfigurationEntity.KeyType.CosmosDbSecondaryReadonly: return "secondaryReadonly";
            }
            return null;
        }
        private static string GetKeyValue(IDatabaseAccountListKeysResult keys, JanitorConfigurationEntity.KeyType type)
        {
            switch (type)
            {
                case JanitorConfigurationEntity.KeyType.CosmosDbPrimary: return keys.PrimaryMasterKey;
                case JanitorConfigurationEntity.KeyType.CosmosDbPrimaryReadonly: return keys.PrimaryReadonlyMasterKey;
                case JanitorConfigurationEntity.KeyType.CosmosDbSecondary: return keys.SecondaryMasterKey;
                case JanitorConfigurationEntity.KeyType.CosmosDbSecondaryReadonly: return keys.SecondaryReadonlyMasterKey;
            }
            return null;
        }
    }
}
