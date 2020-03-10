using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi
{
    public class Dashboard : ProviderIntegratedFunction
    {
        public Dashboard(IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [FunctionName("Dashboard")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard")] HttpRequest req,
            ILogger log)
        {
            if (!PassedHeaderCheck(req)) { log.LogCritical("Attempted to access API without header!"); return new BadRequestResult(); }

            log.LogInformation("Requested Dashboard metrics");

            var allSecrets = await ManagedSecrets.List();
            var expiringInNextWeek = allSecrets.Where(s => DateTimeOffset.Now.AddDays(7) < (s.LastChanged + s.ValidPeriod));
            var expired = allSecrets.Where(s => !s.IsValid);

            var allResources = await Resources.List();
            var allTasks = await RekeyingTasks.List();

            var metrics = new DashboardMetrics()
            {
                TotalResources = allResources.Count,
                TotalSecrets = allSecrets.Count,
                TotalPendingApproval = allTasks.Count,
                TotalExpiringSoon = expiringInNextWeek.Count(),
                TotalExpired = expired.Count(),
                ExpiringSoon = expiringInNextWeek.Select(s => GetViewModel(s)),
                PercentExpired = (int)((double)expired.Count() / allSecrets.Count) * 100,
            };

            foreach (var secret in allSecrets)
            {
                var riskScore = 0;
                foreach (var resourceId in secret.ResourceIds)
                {
                    var resource = await Resources.Get(resourceId);
                    var provider = GetProvider(resource.ProviderType);
                    provider.SerializedConfiguration = resource.ProviderConfiguration;
                    riskScore += provider.GetRisks(secret.ValidPeriod).Sum(r => r.Score);
                }
                if (riskScore > 85)
                    metrics.RiskOver85++;
                else if (riskScore > 60)
                    metrics.Risk85++;
                else if (riskScore > 35)
                    metrics.Risk60++;
                else if (riskScore > 0)
                    metrics.Risk35++;
                else if (riskScore == 0)
                    metrics.Risk0++;
            }

            return new OkObjectResult(metrics);
        }
    }
}
