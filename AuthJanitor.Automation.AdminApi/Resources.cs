﻿using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
    public class Resources : StorageIntegratedFunction
    {
        private readonly ProviderManagerService _providerManager;
        private readonly EventDispatcherService _eventDispatcher;

        public Resources(
            EventDispatcherService eventDispatcher,
            ProviderManagerService providerManager,
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Resource, ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate,
            Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate) :
                base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, scheduleViewModelDelegate, providerViewModelDelegate)
        {
            _eventDispatcher = eventDispatcher;
            _providerManager = providerManager;
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Create")]
        public async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources")] ResourceViewModel resource,
            HttpRequest req)
        {
            if (!req.IsValidUser(AuthJanitorRoles.ResourceAdmin, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            var provider = _providerManager.GetProviderMetadata(resource.ProviderType);
            if (provider == null)
                return new NotFoundObjectResult("Provider type not found");

            if (string.IsNullOrEmpty(resource.SerializedProviderConfiguration))
            {
                resource.SerializedProviderConfiguration = JsonConvert.SerializeObject(
                    resource.ProviderConfiguration.ConfigurationItems.ToDictionary(
                        k => k.Name,
                        v => v.Value));
            }
            try
            {
                // Test deserialization of configuration to make sure it's valid
                var obj = JsonConvert.DeserializeObject(resource.SerializedProviderConfiguration, provider.ProviderConfigurationType);
                if (obj == null) return new BadRequestErrorMessageResult("Invalid Provider configuration");
            }
            catch { return new BadRequestErrorMessageResult("Invalid Provider configuration"); }

            Resource newResource = new Resource()
            {
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = provider.IsRekeyableObjectProvider,
                ProviderType = provider.ProviderTypeName,
                ProviderConfiguration = resource.SerializedProviderConfiguration
            };

            await Resources.CreateAsync(newResource);

            await _eventDispatcher.DispatchEvent(AuthJanitorSystemEvents.ResourceCreated, nameof(AdminApi.Resources.Create), newResource);

            return new OkObjectResult(GetViewModel(newResource));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-List")]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources")] HttpRequest req)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            return new OkObjectResult((await Resources.ListAsync()).Select(r => GetViewModel(r)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Get")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/{resourceId:guid}")] HttpRequest req,
            Guid resourceId)
        {
            if (!req.IsValidUser()) return new UnauthorizedResult();

            if (!await Resources.ContainsIdAsync(resourceId))
            {
                await _eventDispatcher.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.Resources.Get), "Resource not found");
                return new NotFoundResult();
            }

            return new OkObjectResult(GetViewModel(await Resources.GetAsync(resourceId)));
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Delete")]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId:guid}")] HttpRequest req,
            Guid resourceId)
        {
            if (!req.IsValidUser(AuthJanitorRoles.ResourceAdmin, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();

            if (!await Resources.ContainsIdAsync(resourceId))
            {
                await _eventDispatcher.DispatchEvent(AuthJanitorSystemEvents.AnomalousEventOccurred, nameof(AdminApi.Resources.Delete), "Resource not found");
                return new NotFoundResult();
            }

            await Resources.DeleteAsync(resourceId);

            await _eventDispatcher.DispatchEvent(AuthJanitorSystemEvents.ResourceDeleted, nameof(AdminApi.Resources.Delete), resourceId);

            return new OkResult();
        }

        [ProtectedApiEndpoint]
        [FunctionName("Resources-Update")]
        public async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources/{resourceId:guid}")] ResourceViewModel resource,
            HttpRequest req,
            Guid resourceId)
        {
            if (!req.IsValidUser(AuthJanitorRoles.ResourceAdmin, AuthJanitorRoles.GlobalAdmin)) return new UnauthorizedResult();
            
            var provider = _providerManager.GetProviderMetadata(resource.ProviderType);
            if (provider == null)
                return new NotFoundObjectResult("Provider type not found");

            try
            {
                // Test deserialization of configuration to make sure it's valid
                var obj = JsonConvert.DeserializeObject(resource.SerializedProviderConfiguration, provider.ProviderConfigurationType);
                if (obj == null) return new BadRequestErrorMessageResult("Invalid Provider configuration");
            }
            catch { return new BadRequestErrorMessageResult("Invalid Provider configuration"); }

            Resource newResource = new Resource()
            {
                ObjectId = resourceId,
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                ProviderType = resource.ProviderType,
                ProviderConfiguration = resource.SerializedProviderConfiguration
            };

            await Resources.UpdateAsync(newResource);

            await _eventDispatcher.DispatchEvent(AuthJanitorSystemEvents.ResourceUpdated, nameof(AdminApi.Resources.Update), newResource);

            return new OkObjectResult(GetViewModel(newResource));
        }
    }
}
