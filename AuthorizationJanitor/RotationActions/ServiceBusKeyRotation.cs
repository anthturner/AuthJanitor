using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    public class ServiceBusKeyRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var target = newEntity.GetTarget<Targets.NamedResourceWithChildrenTarget>();

            var azure = await HelperMethods.GetAzure();
            var serviceBusNamespace = await azure.ServiceBusNamespaces.GetByResourceGroupAsync(target.ResourceGroup, target.ResourceName);
            var rule = await serviceBusNamespace.AuthorizationRules.GetByNameAsync(target.ChildName);

            var newKey = await rule.RegenerateKeyAsync(GetPolicyKey(entity.Type));
            newEntity.LastChanged = DateTime.Now;
            newEntity.UpdatedKey = GetKeyValue(newKey, entity.Type);

            return newEntity;
        }

        private static Policykey GetPolicyKey(JanitorConfigurationEntity.KeyType type)
        {
            switch (type)
            {
                default:
                case JanitorConfigurationEntity.KeyType.ServiceBusPrimary: return Policykey.PrimaryKey;
                case JanitorConfigurationEntity.KeyType.ServiceBusSecondary: return Policykey.SecondaryKey;
            }
        }

        private static string GetKeyValue(IAuthorizationKeys keys, JanitorConfigurationEntity.KeyType type)
        {
            switch (type)
            {
                default:
                case JanitorConfigurationEntity.KeyType.ServiceBusPrimary: return keys.PrimaryKey;
                case JanitorConfigurationEntity.KeyType.ServiceBusSecondary: return keys.SecondaryKey;
            }
        }
    }
}
