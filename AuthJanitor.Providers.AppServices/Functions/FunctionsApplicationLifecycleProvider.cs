using Microsoft.Azure.Management.AppService.Fluent;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    public abstract class FunctionsApplicationLifecycleProvider<TConsumerConfiguration> : SlottableApplicationLifecycleProvider<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableProviderConfiguration
    {
        protected async Task<IFunctionApp> GetFunctionsApp()
        {
            return await (await GetAzure()).AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);
        }

        protected async Task<IFunctionDeploymentSlot> GetDeploymentSlot(string name)
        {
            return await (await GetFunctionsApp()).DeploymentSlots.GetByNameAsync(name);
        }

        protected async Task PrepareTemporaryDeploymentSlot()
        {
            await (await GetDeploymentSlot(TemporarySlotName)).ApplySlotConfigurationsAsync(SourceSlotName);
        }

        protected async Task SwapTemporaryToDestination()
        {
            await (await GetDeploymentSlot(DestinationSlotName)).SwapAsync(TemporarySlotName);
        }
    }
}
