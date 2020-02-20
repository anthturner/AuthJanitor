using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.AppServices.Functions
{
    public abstract class ConsumingFunctionsApplication<TConsumerConfiguration> : SlottableConsumingApplicationExtension<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableExtensionConfiguration
    {
        protected async Task<IFunctionApp> GetFunctionsApp()
        {
            return await (await GetAzure()).AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);
        }

        protected async Task<IFunctionDeploymentSlot> GetDeploymentSlot(string name)
        {
            return await (await GetFunctionsApp()).DeploymentSlots.GetByNameAsync(name);
        }

        protected ConsumingFunctionsApplication(ILogger logger,
            IRekeyableServiceExtension rekeyableService,
            TConsumerConfiguration configuration) : base(logger, rekeyableService, configuration) { }

        protected async Task SwapSlotsWithUpdate(Func<IFunctionDeploymentSlot, Task> updateAction)
        {
            IFunctionDeploymentSlot temporarySlot = await GetDeploymentSlot(TemporarySlotName);
            await temporarySlot.ApplySlotConfigurationsAsync(SourceSlotName);

            await updateAction(temporarySlot);

            IFunctionDeploymentSlot destinationSlot = await GetDeploymentSlot(DestinationSlotName);
            await destinationSlot.SwapAsync(TemporarySlotName);
        }
    }
}
