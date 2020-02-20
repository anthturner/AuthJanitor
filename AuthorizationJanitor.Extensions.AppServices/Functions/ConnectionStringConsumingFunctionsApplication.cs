using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.AppServices.Functions
{
    public class ConnectionStringConsumingFunctionsApplication : ConsumingFunctionsApplication<ConnectionStringConfiguration>
    {
        public ConnectionStringConsumingFunctionsApplication(ILogger logger,
            IRekeyableServiceExtension rekeyableService,
            ConnectionStringConfiguration configuration) : base(logger, rekeyableService, configuration) { }

        public override async Task Rekey()
        {
            // No pre-work, just go straight to rekeying the Service
            RegeneratedKey newKey = await Service.Rekey();

            // Slot names are brought in already, this function just does the configuration copy and swap.
            await SwapSlotsWithUpdate(async slot =>
            {
                await slot.Update()
                    .WithConnectionString(Configuration.ConnectionStringName, newKey.ConnectionStringOrKey, Configuration.ConnectionStringType)
                    .ApplyAsync();
            });

            // Signal the service that the ConsumingApplication has been swapped to the new key
            await Service.OnConsumingApplicationSwapped();
        }

        public override string GetDescription()
        {
            return $"Update Azure Functions Connection String name '{Configuration.ConnectionStringName}' (Type: '{Configuration.ConnectionStringType.ToString()}')." + Environment.NewLine + base.GetDescription();
        }
    }
}
