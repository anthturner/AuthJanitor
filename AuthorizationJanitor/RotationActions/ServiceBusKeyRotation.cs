using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    /// <summary>
    /// Regenerates a Service Bus key for a given Service Bus RG/Name and Authorization Rule, and commits it to the AppSecrets Key Vault
    /// </summary>
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
            newEntity.UpdatedAppSecret = GetKeyValue(newKey, entity.Type);

            return newEntity;
        }

        private static Policykey GetPolicyKey(JanitorConfigurationEntity.AppSecretType type)
        {
            switch (type)
            {
                default:
                case JanitorConfigurationEntity.AppSecretType.ServiceBusPrimary: return Policykey.PrimaryKey;
                case JanitorConfigurationEntity.AppSecretType.ServiceBusSecondary: return Policykey.SecondaryKey;
            }
        }

        private static string GetKeyValue(IAuthorizationKeys keys, JanitorConfigurationEntity.AppSecretType type)
        {
            switch (type)
            {
                default:
                case JanitorConfigurationEntity.AppSecretType.ServiceBusPrimary: return keys.PrimaryKey;
                case JanitorConfigurationEntity.AppSecretType.ServiceBusSecondary: return keys.SecondaryKey;
            }
        }
    }
}
