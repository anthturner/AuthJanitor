using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
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
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi
{
    /// <summary>
    /// API functions to describe the loaded Providers and their configurations.
    /// A Provider is a library containing logic to either rekey an object/service or manage the lifecycle of an application.
    /// </summary>
    public class Providers : ProviderIntegratedFunction
    {
        public Providers(INotificationProvider notificationProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(notificationProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("Providers-List")]
        public IActionResult List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "providers")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Listing loaded Providers.");

            return new OkObjectResult(LoadedProviders.Select(p => GetViewModel(p)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Providers-GetBlankConfiguration")]
        public IActionResult GetBlankConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "providers/{providerType}")] HttpRequest req,
            string providerType,
            ILogger log)
        {
            log.LogInformation("Retrieving blank configuration for hashed provider name '{0}'.", providerType);

            var provider = LoadedProviders.FirstOrDefault(p => HelperMethods.SHA256HashString(p.ProviderTypeName) == providerType);
            if (provider == null)
                return new BadRequestErrorMessageResult("Invalid Provider type");
            return new OkObjectResult(GetViewModel(GetProviderConfiguration(provider.ProviderTypeName)));
        }
    }
}
