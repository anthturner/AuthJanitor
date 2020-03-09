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
    public class CreateTask : StorageIntegratedFunction
    {
        public CreateTask(IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate) : base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
        }

        [FunctionName("CreateTask")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks")] HttpRequest req,
            [FromBody] RekeyingTask resource,
            ILogger log)
        {
            if (!PassedHeaderCheck(req)) { log.LogCritical("Attempted to access API without header!"); return new BadRequestResult(); }

            log.LogInformation("Creating new Task.");

            var secrets = await ManagedSecrets.List();
            if (resource.ManagedSecretIds.Any(id => !secrets.Any(s => s.ObjectId == id)) ||
                secrets.Where(s => resource.ManagedSecretIds.Contains(s.ObjectId)).Any(s => 
                    !s.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminCachesSignOff) && 
                    !s.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminSignsOffJustInTime)))
            {
                return new BadRequestErrorMessageResult("Invalid Managed Secret ID in set");
            }

            RekeyingTask newTask = new RekeyingTask()
            {
                Queued = DateTimeOffset.Now,
                Expiry = resource.Expiry,
                ManagedSecretIds = resource.ManagedSecretIds
            };

            await RekeyingTasks.Create(newTask);
            return new OkObjectResult(newTask);
        }
    }
}
