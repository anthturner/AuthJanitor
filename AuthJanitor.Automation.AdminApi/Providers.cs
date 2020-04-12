using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi
{
    /// <summary>
    /// API functions to describe the loaded Providers and their configurations.
    /// A Provider is a library containing logic to either rekey an object/service or manage the lifecycle of an application.
    /// </summary>
    public class Providers : ProviderIntegratedFunction
    {
        public Providers(AuthJanitorServiceConfiguration serviceConfiguration, MultiCredentialProvider credentialProvider, EventDispatcherService eventDispatcherService, ISecureStorageProvider secureStorageProvider, IDataStore<ManagedSecret> managedSecretStore, IDataStore<Resource> resourceStore, IDataStore<RekeyingTask> rekeyingTaskStore, Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate, Func<Resource, ResourceViewModel> resourceViewModelDelegate, Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate, Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate, Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate, Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate, Func<string, RekeyingAttemptLogger, IAuthJanitorProvider> providerFactory, Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory, Func<string, LoadedProviderMetadata> providerDetailsFactory, List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, eventDispatcherService, secureStorageProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, scheduleViewModelDelegate, providerViewModelDelegate, providerFactory, providerConfigurationFactory, providerDetailsFactory, loadedProviders)
        {
        }

        [ProtectedApiEndpoint]
        [FunctionName("Providers-List")]
        public IActionResult List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "providers")] HttpRequest req)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            return new OkObjectResult(LoadedProviders.Select(p => GetViewModel(p)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Providers-GetBlankConfiguration")]
        public async Task<IActionResult> GetBlankConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "providers/{providerType}")] HttpRequest req,
            string providerType)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            var provider = LoadedProviders.FirstOrDefault(p => HelperMethods.SHA256HashString(p.ProviderTypeName) == providerType);
            if (provider == null)
            {
                await EventDispatcherService.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.Providers.GetBlankConfiguration), "Invalid Provider specified");
                return new NotFoundResult();
            }
            return new OkObjectResult(GetViewModel(GetProviderConfiguration(provider.ProviderTypeName)));
        }
    }
}
