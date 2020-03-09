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

namespace AuthJanitor.Automation.AdminApi
{
    public class UpdateManagedSecret : StorageIntegratedFunction
    {
        public UpdateManagedSecret(IDataStore<ManagedSecret> managedSecretStore, IDataStore<Shared.Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate) : base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
        }

        [FunctionName("UpdateManagedSecret")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secrets/{secretId:guid}")] ManagedSecretViewModel inputSecret,
            HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            if (!PassedHeaderCheck(req)) { log.LogCritical("Attempted to access API without header!"); return new BadRequestResult(); }

            log.LogInformation("Updating Managed Secret {0}", secretId);

            var secret = await ManagedSecrets.Get(secretId);
            if (secret == null)
                return new BadRequestErrorMessageResult("Secret does not exist!");

            var allResourceIds = (await Resources.List()).Select(r => r.ObjectId);
            if (inputSecret.ResourceIds.Any(r => !allResourceIds.Contains(r)))
            {
                var invalidIds = inputSecret.ResourceIds.Where(r => !allResourceIds.Contains(r));
                log.LogError("New Managed Secret attempted to link one or more invalid Resource IDs: {0}", invalidIds);
                return new BadRequestErrorMessageResult("One or more ResourceIds not found!");
            }

            ManagedSecret newManagedSecret = new ManagedSecret()
            {
                Name = inputSecret.Name,
                Description = inputSecret.Description,
                ValidPeriod = inputSecret.ValidPeriod,
                ResourceIds = inputSecret.ResourceIds
            };

            await ManagedSecrets.Update(newManagedSecret);
            await ManagedSecrets.Commit();

            log.LogInformation("Updated Managed Secret '{0}'", newManagedSecret.Name);

            return new OkObjectResult(GetViewModel(newManagedSecret));
        }
    }
}
