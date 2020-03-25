using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.WebApps
{
    public abstract class WebAppApplicationLifecycleProvider<TConsumerConfiguration> : SlottableApplicationLifecycleProvider<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableProviderConfiguration
    {
        protected WebAppApplicationLifecycleProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
        {
        }

        protected async Task<IWebApp> GetWebApp()
        {
            return await (await GetAzure()).WebApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);
        }

        protected async Task<IDeploymentSlot> GetDeploymentSlot(string name)
        {
            return await (await GetWebApp()).DeploymentSlots.GetByNameAsync(name);
        }
    }
}
