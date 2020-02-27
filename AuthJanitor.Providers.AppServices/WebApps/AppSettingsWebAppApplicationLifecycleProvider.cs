using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.WebAppBase.Update;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.WebApps
{
    [Provider(Name = "WebApp - AppSettings",
              IconClass = "fa fa-globe",
              Description = "Manages the lifecycle of a WebApp which reads from AppSettings")]
    public class AppSettingsWebAppApplicationLifecycleProvider : WebAppApplicationLifecycleProvider<AppSettingConfiguration>
    {
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

            IUpdate<IDeploymentSlot> updateBase = (await GetDeploymentSlot(TemporarySlotName)).Update();
            foreach (RegeneratedSecret secret in newSecrets)
            {
                updateBase = updateBase.WithAppSetting($"{Configuration.SettingName}-{secret.UserHint}", secret.NewSecretValue);
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

        public override IList<RiskyConfigurationItem> GetRisks(TimeSpan requestedValidPeriod)
        {
            return new List<RiskyConfigurationItem>()
            {
                new RiskyConfigurationItem()
                {
                    Risk = "Sample Risk",
                    Recommendation = "This can be safely ignored.",
                    Score = 80
                }
            };
        }

        public override string GetDescription()
        {
            return $"Update Azure WebApps AppSetting name '{Configuration.SettingName}'." + Environment.NewLine + base.GetDescription();
        }
    }
}
