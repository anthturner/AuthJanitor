using AuthJanitor.Providers;
using Microsoft.Azure.Management.AppService.Fluent;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.WebApps
{
    public abstract class WebAppApplicationLifecycleProvider<TConsumerConfiguration> : SlottableApplicationLifecycleProvider<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableProviderConfiguration
    {
        protected WebAppApplicationLifecycleProvider() : base() { }

        protected async Task<IWebApp> GetWebApp() =>
            await (await GetAzure()).WebApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);

        protected async Task<IDeploymentSlot> GetDeploymentSlot(string name) =>
            await (await GetWebApp()).DeploymentSlots.GetByNameAsync(name);

        protected async Task PrepareTemporaryDeploymentSlot() =>
            await (await GetDeploymentSlot(TemporarySlotName)).ApplySlotConfigurationsAsync(SourceSlotName);

        protected async Task SwapTemporaryToDestination() =>
            await (await GetDeploymentSlot(DestinationSlotName)).SwapAsync(TemporarySlotName);
    }
}
