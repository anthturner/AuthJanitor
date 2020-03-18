using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi
{
    /// <summary>
    /// API functions to control the creation and management of AuthJanitor Resources.
    /// A Resource is the description of how to connect to an object or resource, using a given Provider.
    /// </summary>
    public class Resources : ProviderIntegratedFunction
    {
        public Resources(INotificationProvider notificationProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(notificationProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources")] ResourceViewModel resource,
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating new Resource.");

            var provider = GetProviderDetails(resource.ProviderType);
            if (provider == null)
                return new BadRequestErrorMessageResult("Invalid Provider Type");

            try
            {
                // Test deserialization of configuration to make sure it's valid
                var obj = JsonConvert.DeserializeObject(resource.SerializedProviderConfiguration, provider.ProviderConfigurationType);
                if (obj == null) return new BadRequestErrorMessageResult("Invalid Provider configuration");
            }
            catch { return new BadRequestErrorMessageResult("Invalid Provider configuration"); }

            Shared.Resource newResource = new Shared.Resource()
            {
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = provider.IsRekeyableObjectProvider,
                ProviderType = provider.ProviderTypeName,
                ProviderConfiguration = resource.SerializedProviderConfiguration
            };

            await Resources.InitializeAsync();
            Resources.Create(newResource);
            await Resources.CommitAsync();

            return new OkObjectResult(GetViewModel(newResource));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("List all Resource IDs.");

            return new OkObjectResult(Resources.List().Select(r => GetViewModel(r)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/{resourceId:guid}")] HttpRequest req,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Get Resource ID {0}.", resourceId);

            if (!Resources.ContainsId(resourceId))
                return new BadRequestErrorMessageResult("Resource not found!");

            return new OkObjectResult(GetViewModel(Resources.Get(resourceId)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId:guid}")] HttpRequest req,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Deleting Resource ID {0}.", resourceId);

            if (!Resources.ContainsId(resourceId))
                return new BadRequestErrorMessageResult("Resource not found!");
            
            Resources.Delete(resourceId);
            await Resources.CommitAsync();
            return new OkResult();
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Update")]
        public async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources/{resourceId:guid}")] Shared.Resource resource,
            HttpRequest req,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Updating Resource ID {0}.", resourceId);

            try
            {
                // Test deserialization of configuration to make sure it's valid
                var obj = JsonConvert.DeserializeObject(resource.ProviderConfiguration, GetProviderConfiguration(resource.ProviderType).GetType());
                if (obj == null) return new BadRequestErrorMessageResult("Invalid Provider configuration");
            }
            catch { return new BadRequestErrorMessageResult("Invalid Provider configuration"); }

            Shared.Resource newResource = new Shared.Resource()
            {
                ObjectId = resourceId,
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                ProviderType = resource.ProviderType,
                ProviderConfiguration = resource.ProviderConfiguration
            };

            Resources.Update(newResource);
            await Resources.CommitAsync();

            return new OkObjectResult(GetViewModel(newResource));
        }
    }
}
