using Microsoft.Azure.Management.CosmosDB.Fluent;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    /// <summary>
    /// Regenerates one of the CosmosDB keys and commits the new key to the AppSecrets Key Vault
    /// </summary>
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
            newEntity.UpdatedAppSecret = GetKeyValue(keys, entity.Type);
            newEntity.LastChanged = DateTime.Now;

            return newEntity;
        }

        private static string GetKeyName(JanitorConfigurationEntity.AppSecretType type)
        {
            switch (type)
            {
                case JanitorConfigurationEntity.AppSecretType.CosmosDbPrimary: return "primary";
                case JanitorConfigurationEntity.AppSecretType.CosmosDbPrimaryReadonly: return "primaryReadonly";
                case JanitorConfigurationEntity.AppSecretType.CosmosDbSecondary: return "secondary";
                case JanitorConfigurationEntity.AppSecretType.CosmosDbSecondaryReadonly: return "secondaryReadonly";
            }
            return null;
        }
        private static string GetKeyValue(IDatabaseAccountListKeysResult keys, JanitorConfigurationEntity.AppSecretType type)
        {
            switch (type)
            {
                case JanitorConfigurationEntity.AppSecretType.CosmosDbPrimary: return keys.PrimaryMasterKey;
                case JanitorConfigurationEntity.AppSecretType.CosmosDbPrimaryReadonly: return keys.PrimaryReadonlyMasterKey;
                case JanitorConfigurationEntity.AppSecretType.CosmosDbSecondary: return keys.SecondaryMasterKey;
                case JanitorConfigurationEntity.AppSecretType.CosmosDbSecondaryReadonly: return keys.SecondaryReadonlyMasterKey;
            }
            return null;
        }
    }
}
