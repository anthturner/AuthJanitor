using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi
{
    /// <summary>
    /// API functions to control the creation and management of AuthJanitor Managed Secrets.
    /// A Managed Secret is a grouping of Resources and Policies which describe the strategy around rekeying an object and the applications which consume it.
    /// </summary>
    public class ManagedSecrets : StorageIntegratedFunction
    {
        public ManagedSecrets(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, INotificationProvider notificationProvider, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate) : base(serviceConfiguration, credentialProvider, notificationProvider, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, scheduleViewModelDelegate, providerViewModelDelegate)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "managedSecrets")] ManagedSecretViewModel inputSecret,
            HttpRequest req,
            ILogger log)
        {
            if (!req.IsValidUser(AuthJanitorRoles.SecretAdmin, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            log.LogInformation("Creating new Managed Secret");

            var resources = await Resources.ListAsync();
            var resourceIds = inputSecret.ResourceIds.Split(';').Select(r => Guid.Parse(r)).ToList();
            if (resourceIds.Any(id => !resources.Any(r => r.ObjectId == id)))
            {
                var invalidIds = resourceIds.Where(id => !resources.Any(r => r.ObjectId == id));
                log.LogError("New Managed Secret attempted to link one or more invalid Resource IDs: {0}", invalidIds);
                return new BadRequestErrorMessageResult("One or more Resource IDs not found!");
            }

            ManagedSecret newManagedSecret = new ManagedSecret()
            {
                Name = inputSecret.Name,
                Description = inputSecret.Description,
                ValidPeriod = TimeSpan.FromMinutes(inputSecret.ValidPeriodMinutes),
                LastChanged = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(inputSecret.ValidPeriodMinutes),
                TaskConfirmationStrategies = inputSecret.TaskConfirmationStrategies,
                ResourceIds = resourceIds
            };

            await ManagedSecrets.CreateAsync(newManagedSecret);

            log.LogInformation("Created new Managed Secret '{0}'", newManagedSecret.Name);

            return new OkObjectResult(GetViewModel(newManagedSecret));
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "managedSecrets")] HttpRequest req,
            ILogger log)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            log.LogInformation("Listing all Managed Secrets.");

            return new OkObjectResult((await ManagedSecrets.ListAsync()).Select(s => GetViewModel(s)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "managedSecrets/{secretId:guid}")] HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            log.LogInformation("Retrieving Managed Secret {0}.", secretId);

            if (!await ManagedSecrets.ContainsIdAsync(secretId))
                return new BadRequestErrorMessageResult("Secret not found!");

            return new OkObjectResult(GetViewModel(await ManagedSecrets.GetAsync(secretId)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "managedSecrets/{secretId:guid}")] HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            if (!req.IsValidUser(AuthJanitorRoles.SecretAdmin, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            log.LogInformation("Deleting Managed Secret {0}", secretId);

            if (!await ManagedSecrets.ContainsIdAsync(secretId))
                return new BadRequestErrorMessageResult("Secret not found!");

            await ManagedSecrets.DeleteAsync(secretId);

            log.LogInformation("Deleted Managed Secret {0}", secretId);

            return new OkResult();
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Update")]
        public async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "managedSecrets/{secretId:guid}")] ManagedSecretViewModel inputSecret,
            HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            if (!req.IsValidUser(AuthJanitorRoles.SecretAdmin, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            log.LogInformation("Updating Managed Secret {0}", secretId);

            if (!await ManagedSecrets.ContainsIdAsync(secretId))
                return new BadRequestErrorMessageResult("Secret not found!");

            var resources = await Resources.ListAsync();
            var resourceIds = inputSecret.ResourceIds.Split(';').Select(r => Guid.Parse(r)).ToList();
            if (resourceIds.Any(id => !resources.Any(r => r.ObjectId == id)))
            {
                var invalidIds = resourceIds.Where(id => !resources.Any(r => r.ObjectId == id));
                log.LogError("New Managed Secret attempted to link one or more invalid Resource IDs: {0}", invalidIds);
                return new BadRequestErrorMessageResult("One or more ResourceIds not found!");
            }

            ManagedSecret newManagedSecret = new ManagedSecret()
            {
                ObjectId = secretId,
                Name = inputSecret.Name,
                Description = inputSecret.Description,
                ValidPeriod = TimeSpan.FromMinutes(inputSecret.ValidPeriodMinutes),
                TaskConfirmationStrategies = inputSecret.TaskConfirmationStrategies,
                ResourceIds = resourceIds
            };

            await ManagedSecrets.UpdateAsync(newManagedSecret);

            log.LogInformation("Updated Managed Secret '{0}'", newManagedSecret.Name);

            return new OkObjectResult(GetViewModel(newManagedSecret));
        }
    }
}
