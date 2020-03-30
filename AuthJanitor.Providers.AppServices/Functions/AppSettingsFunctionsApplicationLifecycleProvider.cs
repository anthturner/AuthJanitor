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
            Logger.LogInformation("Moving slot configuration from '{0}' to '{1}'", SourceSlotName, TemporarySlotName);
            await (await GetDeploymentSlot(TemporarySlotName)).ApplySlotConfigurationsAsync(SourceSlotName);
            if (temporaryUseSecrets.Count > 1 && temporaryUseSecrets.Select(s => s.UserHint).Distinct().Count() != temporaryUseSecrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (RegeneratedSecret secret in temporaryUseSecrets)
            {
                var appSettingName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.SettingName : $"{Configuration.SettingName}-{secret.UserHint}";
                Logger.LogInformation("Updating AppSetting '{0}' in slot '{1}' (as {2})", appSettingName, TemporarySlotName,
                    Configuration.CommitAsConnectionString ? "connection string" : "secret");
                updateBase = updateBase.WithoutAppSetting(appSettingName);
                if (Configuration.CommitAsConnectionString)
                    updateBase = updateBase.WithAppSetting(appSettingName, secret.NewConnectionStringOrKey);
                else
                    updateBase = updateBase.WithAppSetting(appSettingName, secret.NewSecretValue);
            }

            Logger.LogInformation("Applying changes.");
            await updateBase.ApplyAsync();

            Logger.LogInformation("Swapping to '{0}'", TemporarySlotName);
            await (await GetDeploymentSlot(TemporarySlotName)).SwapAsync(SourceSlotName);
            Logger.LogInformation("BeforeRekeying completed!");
        }

        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            Logger.LogInformation("Moving slot configuration from '{0}' to '{1}'", TemporarySlotName, DestinationSlotName);
            await (await GetDeploymentSlot(DestinationSlotName)).ApplySlotConfigurationsAsync(TemporarySlotName);
            if (newSecrets.Count > 1 && newSecrets.Select(s => s.UserHint).Distinct().Count() != newSecrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(DestinationSlotName)).Update();
            foreach (RegeneratedSecret secret in newSecrets)
            {
                Logger.LogInformation("Updating AppSetting '{0}' in slot '{1}'", Configuration.SettingName, DestinationSlotName);
                var secretName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.SettingName : $"{Configuration.SettingName}-{secret.UserHint}";
                updateBase = updateBase.WithoutAppSetting(secretName);
                if (Configuration.CommitAsConnectionString)
                    updateBase = updateBase.WithAppSetting(secretName, secret.NewConnectionStringOrKey);
                else
                    updateBase = updateBase.WithAppSetting(secretName, secret.NewSecretValue);
            }

            Logger.LogInformation("Applying changes.");
            await updateBase.ApplyAsync();
            Logger.LogInformation("CommitNewSecrets completed!");
        }

        /// <summary>
        /// Call after all new keys have been committed
        /// </summary>
        public override async Task AfterRekeying()
        {
            Logger.LogInformation("Swapping to '{0}'", DestinationSlotName);
            await (await GetDeploymentSlot(DestinationSlotName)).SwapAsync(TemporarySlotName);
            Logger.LogInformation("Swap complete!");
        }

        public override string GetDescription() =>
            $"Populates an App Setting called '{Configuration.SettingName}' in an Azure " +
            $"Functions application called {Configuration.ResourceName} (Resource Group " +
            $"'{Configuration.ResourceGroup}'). During the rekeying, the Functions App will " +
            $"be moved from slot '{Configuration.SourceSlot}' to slot '{Configuration.TemporarySlot}' " +
            $"temporarily, and then to slot '{Configuration.DestinationSlot}'.";
    }
}
