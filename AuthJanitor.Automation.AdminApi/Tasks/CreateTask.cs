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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks")] string[] secretIds,
            HttpRequest req,
            ILogger log)
        {
            if (!PassedHeaderCheck(req)) { log.LogCritical("Attempted to access API without header!"); return new BadRequestResult(); }

            log.LogInformation("Creating new Task.");

            var secrets = await ManagedSecrets.List();
            if (secretIds.Any(id => !secrets.Any(s => s.ObjectId == Guid.Parse(id))) ||
                secrets.Where(s => secretIds.Contains(s.ObjectId.ToString())).Any(s => 
                    !s.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminCachesSignOff) && 
                    !s.TaskConfirmationStrategies.HasFlag(TaskConfirmationStrategies.AdminSignsOffJustInTime)))
            {
                return new BadRequestErrorMessageResult("Invalid Managed Secret ID in set");
            }

            var earliestExpiry = secrets.Where(s => secretIds.Contains(s.ObjectId.ToString()))
                .Min(s => s.LastChanged + s.ValidPeriod);

            RekeyingTask newTask = new RekeyingTask()
            {
                Queued = DateTimeOffset.Now,
                Expiry = earliestExpiry,
                ManagedSecretIds = secretIds.Select(s => Guid.Parse(s)).ToList()
            };

            await RekeyingTasks.Create(newTask);
            await RekeyingTasks.Commit();

            return new OkObjectResult(newTask);
        }
    }
}
