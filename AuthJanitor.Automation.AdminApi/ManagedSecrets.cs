using AuthJanitor.Automation.Shared;
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
        public ManagedSecrets(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, INotificationProvider notificationProvider, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate) : base(serviceConfiguration, credentialProvider, notificationProvider, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secrets")] ManagedSecretViewModel inputSecret,
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating new Managed Secret");

            var resourceIds = inputSecret.ResourceIds.Split(';').Select(r => Guid.Parse(r)).ToList();
            if (resourceIds.Any(r => !Resources.ContainsId(r)))
            {
                var invalidIds = resourceIds.Where(r => !Resources.ContainsId(r));
                log.LogError("New Managed Secret attempted to link one or more invalid Resource IDs: {0}", invalidIds);
                return new BadRequestErrorMessageResult("One or more ResourceIds not found!");
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

            await ManagedSecrets.InitializeAsync(); // reload before committing
            ManagedSecrets.Create(newManagedSecret);
            await ManagedSecrets.CommitAsync();

            log.LogInformation("Created new Managed Secret '{0}'", newManagedSecret.Name);

            return new OkObjectResult(GetViewModel(newManagedSecret));
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secrets")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Listing all Managed Secrets.");

            return new OkObjectResult(ManagedSecrets.List().Select(s => GetViewModel(s)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secrets/{secretId:guid}")] HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            log.LogInformation("Retrieving Managed Secret {0}.", secretId);

            if (!ManagedSecrets.ContainsId(secretId))
                return new BadRequestErrorMessageResult("Secret not found!");

            return new OkObjectResult(GetViewModel(ManagedSecrets.Get(secretId)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "secrets/{secretId:guid}")] HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            log.LogInformation("Deleting Managed Secret {0}", secretId);

            if (!ManagedSecrets.ContainsId(secretId))
                return new BadRequestErrorMessageResult("Secret not found!");

            ManagedSecrets.Delete(secretId);
            await ManagedSecrets.CommitAsync();

            log.LogInformation("Deleted Managed Secret {0}", secretId);

            return new OkResult();
        }
        
        [ProtectedApiEndpoint]
        [FunctionName("ManagedSecrets-Update")]
        public async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secrets/{secretId:guid}")] ManagedSecretViewModel inputSecret,
            HttpRequest req,
            Guid secretId,
            ILogger log)
        {
            log.LogInformation("Updating Managed Secret {0}", secretId);

            if (!ManagedSecrets.ContainsId(secretId))
                return new BadRequestErrorMessageResult("Secret not found!");

            var resourceIds = inputSecret.ResourceIds.Split(';').Select(r => Guid.Parse(r)).ToList();
            if (resourceIds.Any(r => !Resources.ContainsId(r)))
            {
                var invalidIds = resourceIds.Where(r => !Resources.ContainsId(r));
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

            await ManagedSecrets.InitializeAsync(); // reload before committing
            ManagedSecrets.Update(newManagedSecret);
            await ManagedSecrets.CommitAsync();

            log.LogInformation("Updated Managed Secret '{0}'", newManagedSecret.Name);

            return new OkObjectResult(GetViewModel(newManagedSecret));
        }
    }
}
