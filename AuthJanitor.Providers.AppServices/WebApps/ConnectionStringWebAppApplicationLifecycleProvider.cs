using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.WebAppBase.Update;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.WebApps
{
    [Provider(Name = "WebApp - Connection String",
              IconClass = "fa fa-globe",
              Description = "Manages the lifecycle of an Azure Web App which reads from a Connection String")]
    [ProviderImage(ProviderImages.WEBAPPS_SVG)]
    public class ConnectionStringWebAppApplicationLifecycleProvider : WebAppApplicationLifecycleProvider<ConnectionStringConfiguration>
    {
        public ConnectionStringWebAppApplicationLifecycleProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        /// <summary>
        /// Call to prepare the application for a new secret, passing in a secret
        /// which will be valid while the Rekeying is taking place (for zero-downtime)
        /// </summary>
        public override async Task BeforeRekeying(List<RegeneratedSecret> temporaryUseSecrets)
        {
            await(await GetDeploymentSlot(TemporarySlotName)).ApplySlotConfigurationsAsync(SourceSlotName);
            if (temporaryUseSecrets.Count > 1 && temporaryUseSecrets.Select(s => s.UserHint).Distinct().Count() != temporaryUseSecrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (RegeneratedSecret secret in temporaryUseSecrets)
            {
                var connectionStringName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.ConnectionStringName : $"{Configuration.ConnectionStringName}-{secret.UserHint}";
                updateBase = updateBase.WithoutConnectionString(connectionStringName);
                updateBase = updateBase.WithAppSetting(connectionStringName, secret.NewConnectionStringOrKey);
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
            if (newSecrets.Count > 1 && newSecrets.Select(s => s.UserHint).Distinct().Count() != newSecrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (RegeneratedSecret secret in newSecrets)
            {
                updateBase = updateBase.WithConnectionString($"{Configuration.ConnectionStringName}-{secret.UserHint}",
                    secret.NewConnectionStringOrKey,
                    Configuration.ConnectionStringType);
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
            $"Populates a Connection String for '{Configuration.ConnectionStringType}' called " +
            $"'{Configuration.ConnectionStringName}' in an Azure " +
            $"Web Application called {Configuration.ResourceName} (Resource Group " +
            $"'{Configuration.ResourceGroup}'). During the rekeying, the Functions App will " +
            $"be moved from slot '{Configuration.SourceSlot}' to slot '{Configuration.TemporarySlot}' " +
            $"temporarily, and then to slot '{Configuration.DestinationSlot}'.";
    }
}
