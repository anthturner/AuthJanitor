using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    public abstract class FunctionsApplicationLifecycleProvider<TConsumerConfiguration> : SlottableApplicationLifecycleProvider<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableProviderConfiguration
    {
        protected FunctionsApplicationLifecycleProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public override async Task Test()
        {
            var sourceDeploymentSlot = await GetDeploymentSlot(SourceSlotName);
            if (sourceDeploymentSlot == null) throw new Exception("Source Deployment Slot is invalid");

            var temporaryDeploymentSlot = await GetDeploymentSlot(TemporarySlotName);
            if (temporaryDeploymentSlot == null) throw new Exception("Temporary Deployment Slot is invalid");

            var destinationDeploymentSlot = await GetDeploymentSlot(DestinationSlotName);
            if (destinationDeploymentSlot == null) throw new Exception("Destination Deployment Slot is invalid");
        }

        protected Task<IFunctionApp> GetFunctionsApp() =>
            GetAzure().ContinueWith(az => az.Result.AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName)).Unwrap();

        protected Task<IFunctionDeploymentSlot> GetDeploymentSlot(string name) =>
            GetFunctionsApp().ContinueWith(az => az.Result.DeploymentSlots.GetByNameAsync(name)).Unwrap();
    }
}
