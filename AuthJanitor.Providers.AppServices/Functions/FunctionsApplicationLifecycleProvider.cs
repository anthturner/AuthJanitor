﻿using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    public abstract class FunctionsApplicationLifecycleProvider<TConsumerConfiguration> : SlottableApplicationLifecycleProvider<TConsumerConfiguration>
        where TConsumerConfiguration : SlottableProviderConfiguration
    {
        private const string PRODUCTION_SLOT_NAME = "production";
        protected FunctionsApplicationLifecycleProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public override async Task Test()
        {
            if (SourceSlotName != PRODUCTION_SLOT_NAME)
                await TestSlot(SourceSlotName);
            if (TemporarySlotName != PRODUCTION_SLOT_NAME)
                await TestSlot(TemporarySlotName);
            if (DestinationSlotName != PRODUCTION_SLOT_NAME)
                await TestSlot(DestinationSlotName);
        }
        private async Task TestSlot(string slotName)
        {
            var resolvedSlotObject = await GetDeploymentSlot(slotName);
            if (resolvedSlotObject == null) throw new Exception($"Invalid slot name '{slotName}'");
        }

        protected Task<IFunctionApp> GetFunctionsApp() =>
            GetAzure().ContinueWith(az => az.Result.AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName)).Unwrap();

        protected Task<IFunctionDeploymentSlot> GetDeploymentSlot(string name) =>
            GetFunctionsApp().ContinueWith(az => az.Result.DeploymentSlots.GetByNameAsync(name)).Unwrap();
    }
}
