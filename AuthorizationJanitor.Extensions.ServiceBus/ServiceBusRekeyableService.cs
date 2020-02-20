using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.ServiceBus
{
    public class ServiceBusRekeyableService : RekeyableServiceExtension<ServiceBusKeyConfiguration>
    {
        public ServiceBusRekeyableService(ILogger logger, ServiceBusKeyConfiguration configuration) : base(logger, configuration) { }

        public override async Task<RegeneratedKey> Rekey()
        {
            IAuthorizationKeys newKeys = await Regenerate(GetPolicyKey());
            return new RegeneratedKey()
            {
                NewKey = GetKeyValue(newKeys),
                ConnectionString = GetConnectionStringValue(newKeys)
            };
        }

        public override async Task OnConsumingApplicationSwapped()
        {
            if (!Configuration.SkipScramblingOtherKey)
            {
                await Regenerate(GetOtherPolicyKey());
            }
        }

        private async Task<IAuthorizationKeys> Regenerate(Policykey keyType)
        {
            IServiceBusNamespace serviceBusNamespace = await (await GetAzure()).ServiceBusNamespaces.GetByResourceGroupAsync(ResourceGroup, ResourceName);
            INamespaceAuthorizationRule authRule = await serviceBusNamespace.AuthorizationRules.GetByNameAsync(Configuration.AuthorizationRuleName);
            return await authRule.RegenerateKeyAsync(keyType);
        }

        private Policykey GetPolicyKey()
        {
            return Configuration.KeyType == ServiceBusKeyConfiguration.ServiceBusKeyTypes.Primary ?
Policykey.PrimaryKey : Policykey.SecondaryKey;
        }

        private Policykey GetOtherPolicyKey()
        {
            return Configuration.KeyType == ServiceBusKeyConfiguration.ServiceBusKeyTypes.Secondary ?
Policykey.PrimaryKey : Policykey.SecondaryKey;
        }

        private string GetKeyValue(IAuthorizationKeys keys)
        {
            return Configuration.KeyType == ServiceBusKeyConfiguration.ServiceBusKeyTypes.Primary ?
keys.PrimaryKey : keys.SecondaryKey;
        }

        private string GetConnectionStringValue(IAuthorizationKeys keys)
        {
            return Configuration.KeyType == ServiceBusKeyConfiguration.ServiceBusKeyTypes.Primary ?
keys.PrimaryConnectionString : keys.SecondaryConnectionString;
        }
    }
}