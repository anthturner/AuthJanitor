using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.ServiceBus
{
    [Provider(Name = "Service Bus Key",
              IconClass = "fa fa-key",
              Description = "Regenerates an Azure Service Bus Key")]
    [ProviderImage(ProviderImages.SERVICE_BUS_SVG)]
    public class ServiceBusRekeyableObjectProvider : RekeyableObjectProvider<ServiceBusKeyConfiguration>
    {
        public ServiceBusRekeyableObjectProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
        {
        }

        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            IAuthorizationKeys newKeys = await Regenerate(GetPolicyKey());
            return new RegeneratedSecret()
            {
                Expiry = DateTimeOffset.Now + requestedValidPeriod,
                UserHint = Configuration.UserHint,
                NewSecretValue = GetKeyValue(newKeys),
                NewConnectionString = GetConnectionStringValue(newKeys)
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