using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi.Tasks
{
    public class ApproveTask : ProviderIntegratedFunction
    {
        public ApproveTask(IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [FunctionName("ApproveTask")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks/{taskId:guid}/approve")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            if (!PassedHeaderCheck(req)) { log.LogCritical("Attempted to access API without header!"); return new BadRequestResult(); }

            log.LogInformation("Administrator approved Task ID {0}", taskId);

            RekeyingTask task = await RekeyingTasks.Get(taskId);
            Dictionary<Guid, string> secretResults = new Dictionary<Guid, string>();

            if (task.Expiry < DateTime.Now)
            {
                log.LogError("Expiry time has passed; this rekeying operation may be a little bumpy!");
            }

            var allResourceIds = (await Resources.List()).Select(r => r.ObjectId);
            foreach (Guid managedSecretId in task.ManagedSecretIds)
            {
                try
                {
                    log.LogInformation("Rekeying Managed Secret ID {0}", managedSecretId);
                    ManagedSecret secret = await ManagedSecrets.Get(managedSecretId);

                    if (secret.ResourceIds.Any(id => !allResourceIds.Contains(id)))
                    {
                        return new BadRequestErrorMessageResult("Invalid Resource ID in set");
                    }

                    var providers =
                        secret.ResourceIds.Select(id => Resources.Get(id))
                                          .Select(t => t.Result)
                                          .Select(r =>
                                          {
                                              var provider = GetProvider(r.ProviderType);
                                              provider.SerializedConfiguration = r.ProviderConfiguration;
                                              return provider;
                                          }).ToArray();

                    await HelperMethods.RunRekeyingWorkflow(log, secret.ValidPeriod, providers);
                    secretResults.Add(managedSecretId, "Success");
                }
                catch (Exception ex)
                {
                    secretResults.Add(managedSecretId, $"Error: {ex.Message}");
                }
            }

            return new OkObjectResult(secretResults);
        }
    }
}
