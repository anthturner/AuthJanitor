using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi
{
    public abstract class StorageIntegratedFunction
    {
        private Func<ManagedSecret, ManagedSecretViewModel> _managedSecretViewModelDelegate;
        private Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> _resourceViewModelDelegate;
        private Func<RekeyingTask, RekeyingTaskViewModel> _rekeyingTaskViewModelDelegate;
        private Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> _configViewModelDelegate;
        private Func<LoadedProviderMetadata, LoadedProviderViewModel> _providerViewModelDelegate;

        protected IDataStore<ManagedSecret> ManagedSecrets { get; }
        protected IDataStore<Shared.Resource> Resources { get; }
        protected IDataStore<RekeyingTask> RekeyingTasks { get; }

        protected ManagedSecretViewModel GetViewModel(ManagedSecret secret) => _managedSecretViewModelDelegate(secret);
        protected Shared.ViewModels.ResourceViewModel GetViewModel(Shared.Resource resource) => _resourceViewModelDelegate(resource);
        protected RekeyingTaskViewModel GetViewModel(RekeyingTask rekeyingTask) => _rekeyingTaskViewModelDelegate(rekeyingTask);
        protected IEnumerable<ProviderConfigurationItemViewModel> GetViewModel(AuthJanitorProviderConfiguration config) => _configViewModelDelegate(config);
        protected LoadedProviderViewModel GetViewModel(LoadedProviderMetadata provider) => _providerViewModelDelegate(provider);

        protected bool PassedHeaderCheck(HttpRequest req) => req.Headers.ContainsKey("AuthJanitor") && req.Headers["AuthJanitor"].First() == "administrator";

        protected StorageIntegratedFunction(
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Shared.Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate)
        {
            ManagedSecrets = managedSecretStore;
            Resources = resourceStore;
            RekeyingTasks = rekeyingTaskStore;

            _managedSecretViewModelDelegate = managedSecretViewModelDelegate;
            _resourceViewModelDelegate = resourceViewModelDelegate;
            _rekeyingTaskViewModelDelegate = rekeyingTaskViewModelDelegate;
            _configViewModelDelegate = configViewModelDelegate;
            _providerViewModelDelegate = providerViewModelDelegate;
        }
    }
}
