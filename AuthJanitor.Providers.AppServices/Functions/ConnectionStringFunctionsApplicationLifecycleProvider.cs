﻿using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.WebAppBase.Update;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    [Provider(Name = "Functions App - Connection String",
              IconClass = "fa fa-gears",
              Description = "Manages the lifecycle of an Azure Functions app which reads from a Connection String")]
    [ProviderImage(ProviderImages.FUNCTIONS_SVG)]
    public class ConnectionStringFunctionsApplicationLifecycleProvider : FunctionsApplicationLifecycleProvider<ConnectionStringConfiguration>
    {
        public ConnectionStringFunctionsApplicationLifecycleProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        /// <summary>
        /// Call to prepare the application for a new secret, passing in a secret
        /// which will be valid while the Rekeying is taking place (for zero-downtime)
        /// </summary>
        public override async Task BeforeRekeying(List<RegeneratedSecret> temporaryUseSecrets)
        {
            await ApplySecrets(TemporarySlotName, temporaryUseSecrets);
            Logger.LogInformation("BeforeRekeying completed!");
        }

        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            await ApplySecrets(TemporarySlotName, newSecrets);
            Logger.LogInformation("CommitNewSecrets completed!");
        }

        /// <summary>
        /// Call after all new keys have been committed
        /// </summary>
        public override async Task AfterRekeying()
        {
            Logger.LogInformation("Swapping to '{0}'", TemporarySlotName);
            await (await GetFunctionsApp()).SwapAsync(TemporarySlotName);
            Logger.LogInformation("Swap complete!");
        }

        public override string GetDescription() =>
            $"Populates a Connection String for '{Configuration.ConnectionStringType}' called " +
            $"'{Configuration.ConnectionStringName}' in an Azure " +
            $"Functions application called {Configuration.ResourceName} (Resource Group " +
            $"'{Configuration.ResourceGroup}'). During the rekeying, the Functions App will " +
            $"be moved from slot '{Configuration.SourceSlot}' to slot '{Configuration.TemporarySlot}' " +
            $"temporarily, and then to slot '{Configuration.DestinationSlot}'.";

        private async Task ApplySecrets(string slotName, List<RegeneratedSecret> secrets)
        {
            if (secrets.Count > 1 && secrets.Select(s => s.UserHint).Distinct().Count() != secrets.Count)
            {
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");
            }

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (RegeneratedSecret secret in secrets)
            {
                var connectionStringName = string.IsNullOrEmpty(secret.UserHint) ? Configuration.ConnectionStringName : $"{Configuration.ConnectionStringName}-{secret.UserHint}";
                Logger.LogInformation("Updating Connection String '{0}' in slot '{1}'", connectionStringName, TemporarySlotName);
                updateBase = updateBase.WithoutConnectionString(connectionStringName);
                updateBase = updateBase.WithConnectionString(connectionStringName, secret.NewConnectionStringOrKey, Configuration.ConnectionStringType);
            }

            Logger.LogInformation("Applying changes.");
            await updateBase.ApplyAsync();

            Logger.LogInformation("Swapping to '{0}'", TemporarySlotName);
            await (await GetFunctionsApp()).SwapAsync(TemporarySlotName);
        }
    }
}
