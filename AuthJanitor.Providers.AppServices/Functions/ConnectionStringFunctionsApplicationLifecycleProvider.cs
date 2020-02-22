﻿using AuthJanitor.Providers;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.WebAppBase.Update;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    public class ConnectionStringFunctionsApplicationLifecycleProvider : FunctionsApplicationLifecycleProvider<ConnectionStringConfiguration>
    {
        /// <summary>
        /// Call to prepare the application for a new secret
        /// </summary>
        public override Task BeforeRekeying() => PrepareTemporaryDeploymentSlot();

        /// <summary>
        /// Call to commit the newly generated secret
        /// </summary>
        public override async Task CommitNewSecrets(List<RegeneratedSecret> newSecrets)
        {
            if (newSecrets.Count > 1 && newSecrets.Select(s => s.UserHint).Distinct().Count() != newSecrets.Count)
                throw new Exception("Multiple secrets sent to Provider but without distinct UserHints!");

            IUpdate<IFunctionDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (var secret in newSecrets)
                updateBase = updateBase.WithConnectionString($"{Configuration.ConnectionStringName}-{secret.UserHint}",
                    secret.NewConnectionStringOrKey,
                    Configuration.ConnectionStringType);
            await updateBase.ApplyAsync();
        }

        /// <summary>
        /// Call after all new keys have been committed
        /// </summary>
        public override Task AfterRekeying() => SwapTemporaryToDestination();

        public override string GetDescription()
        {
            return $"Update Azure Functions Connection String name '{Configuration.ConnectionStringName}' (Type: '{Configuration.ConnectionStringType.ToString()}')." + Environment.NewLine + base.GetDescription();
        }
    }
}
