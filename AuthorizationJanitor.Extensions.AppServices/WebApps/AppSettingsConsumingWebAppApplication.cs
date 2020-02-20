using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.AppServices.WebApps
{
    public class AppSettingsConsumingWebAppApplication : ConsumingWebAppApplication<AppSettingConfiguration>
    {
        public AppSettingsConsumingWebAppApplication(ILogger logger,
            IRekeyableServiceExtension rekeyableService,
            AppSettingConfiguration configuration) : base(logger, rekeyableService, configuration) { }

        public override async Task Rekey()
        {
            // No pre-work, just go straight to rekeying the Service
            RegeneratedKey newKey = await Service.Rekey();

            // Slot names are brought in already, this function just does the configuration copy and swap.
            await SwapSlotsWithUpdate(async slot =>
            {
                await slot.Update()
                    .WithAppSetting(Configuration.SettingName, newKey.NewKey)
                    .ApplyAsync();
            });

            // Signal the service that the ConsumingApplication has been swapped to the new key
            await Service.OnConsumingApplicationSwapped();
        }

        public override string GetDescription()
        {
            return $"Update Azure WebApps AppSetting name '{Configuration.SettingName}'." + Environment.NewLine + base.GetDescription();
        }
    }
}
