using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.AppServices.WebApps
{
    public abstract class ConsumingWebAppApplication<TConsumerConfiguration> : SlottableConsumingApplicationExtension<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableExtensionConfiguration
    {
        protected async Task<IWebApp> GetWebApp()
        {
            return await (await GetAzure()).AppServices.WebApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);
        }

        protected async Task<IDeploymentSlot> GetDeploymentSlot(string name)
        {
            return await (await GetWebApp()).DeploymentSlots.GetByNameAsync(name);
        }

        protected ConsumingWebAppApplication(ILogger logger,
            IRekeyableServiceExtension rekeyableService,
            TConsumerConfiguration configuration) : base(logger, rekeyableService, configuration) { }

        protected async Task SwapSlotsWithUpdate(Func<IDeploymentSlot, Task> updateAction)
        {
            IDeploymentSlot temporarySlot = await GetDeploymentSlot(TemporarySlotName);
            await temporarySlot.ApplySlotConfigurationsAsync(SourceSlotName);

            await updateAction(temporarySlot);

            IDeploymentSlot destinationSlot = await GetDeploymentSlot(DestinationSlotName);
            await destinationSlot.SwapAsync(TemporarySlotName);
        }
    }
}
