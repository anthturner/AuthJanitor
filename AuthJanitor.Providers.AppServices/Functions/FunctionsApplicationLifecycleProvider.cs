using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    public abstract class FunctionsApplicationLifecycleProvider<TConsumerConfiguration> : SlottableApplicationLifecycleProvider<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableProviderConfiguration
    {
        protected FunctionsApplicationLifecycleProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
        {
        }

        protected Task<IFunctionApp> GetFunctionsApp() =>
            GetAzure().ContinueWith(az => az.Result.AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName)).Unwrap();

        protected Task<IFunctionDeploymentSlot> GetDeploymentSlot(string name) =>
            GetFunctionsApp().ContinueWith(az => az.Result.DeploymentSlots.GetByNameAsync(name)).Unwrap();
    }
}
