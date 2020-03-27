using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.WebAppBase.Update;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    /// <summary>
    /// Defines a Functions application which receives key information through an AppConfig setting
    /// </summary>
    [Provider(Name = "Functions App - AppSettings",
              IconClass = "fa fa-gears",
              Description = "Manages the lifecycle of an Azure Functions app which reads a Managed Secret from its Application Settings")]
    [ProviderImage(ProviderImages.FUNCTIONS_SVG)]
    public class AppSettingsFunctionsApplicationLifecycleProvider : FunctionsApplicationLifecycleProvider<AppSettingConfiguration>
    {
        public AppSettingsFunctionsApplicationLifecycleProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        /// <summary>
        /// Call to prepare the application for a new secret, passing in a secret
        /// which will be valid while the Rekeying is taking place (for zero-downtime)
        /// </summary>
        public override async Task BeforeRekeying(List<RegeneratedSecret> temporaryUseSecrets)
        {
            await (await GetDeploymentSlot(TemporarySlotName)).ApplySlotConfigurationsAsync(SourceSlotName);
            if (temporaryUseSecrets.Count > 1 && temporaryUseSecrets.Select(s => s.UserHint).Distinct().Count() != temporaryUseSecrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (RegeneratedSecret secret in temporaryUseSecrets)
            {
                var secretName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.SettingName : $"{Configuration.SettingName}-{secret.UserHint}";
                updateBase = updateBase.WithoutAppSetting(secretName);
                updateBase = updateBase.WithAppSetting(secretName, secret.NewSecretValue);
            }

            await updateBase.ApplyAsync();

            // Swap to Temporary (which has temp key)
            await (await GetDeploymentSlot(TemporarySlotName)).SwapAsync(SourceSlotName);
        }

        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            await (await GetDeploymentSlot(DestinationSlotName)).ApplySlotConfigurationsAsync(TemporarySlotName);
            if (newSecrets.Count > 1 && newSecrets.Select(s => s.UserHint).Distinct().Count() != newSecrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(DestinationSlotName)).Update();
            foreach (RegeneratedSecret secret in newSecrets)
            {
                var secretName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.SettingName : $"{Configuration.SettingName}-{secret.UserHint}";
                updateBase = updateBase.WithoutAppSetting(secretName);
                updateBase = updateBase.WithAppSetting(secretName, secret.NewSecretValue);
            }

            await updateBase.ApplyAsync();
        }

        /// <summary>
        /// Call after all new keys have been committed
        /// </summary>
        public override async Task AfterRekeying()
        {
            await (await GetDeploymentSlot(DestinationSlotName)).SwapAsync(TemporarySlotName);
        }

        public override string GetDescription() =>
            $"Populates an App Setting called '{Configuration.SettingName}' in an Azure " +
            $"Functions application called {Configuration.ResourceName} (Resource Group " +
            $"'{Configuration.ResourceGroup}'). During the rekeying, the Functions App will " +
            $"be moved from slot '{Configuration.SourceSlot}' to slot '{Configuration.TemporarySlot}' " +
            $"temporarily, and then to slot '{Configuration.DestinationSlot}'.";
    }
}
