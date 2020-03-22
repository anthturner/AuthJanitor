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
        public ConnectionStringFunctionsApplicationLifecycleProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
        {
        }

        /// <summary>
        /// Call to prepare the application for a new secret
        /// </summary>
        public override Task BeforeRekeying()
        {
            return PrepareTemporaryDeploymentSlot();
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

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
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
        public override Task AfterRekeying()
        {
            return SwapTemporaryToDestination();
        }

        public override string GetDescription()
        {
            return $"Update Azure Functions Connection String name '{Configuration.ConnectionStringName}' (Type: '{Configuration.ConnectionStringType.ToString()}')." + Environment.NewLine + base.GetDescription();
        }
    }
}
