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

namespace AuthJanitor.Automation.AdminApi
{
    public class ListManagedSecrets : StorageIntegratedFunction
    {
        public ListManagedSecrets(IDataStore<ManagedSecret> managedSecretStore, IDataStore<Shared.Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate) : base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
        }

        [FunctionName("ListManagedSecrets")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secrets")] HttpRequest req,
            ILogger log)
        {
            if (!PassedHeaderCheck(req)) { log.LogCritical("Attempted to access API without header!"); return new BadRequestResult(); }

            log.LogInformation("Listing all Managed Secrets.");

            return new OkObjectResult((await ManagedSecrets.List()).Select(s => GetViewModel(s)));
        }
    }
}
