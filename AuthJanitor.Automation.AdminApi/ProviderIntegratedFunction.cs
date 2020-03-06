using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.AdminApi
{
    public abstract class ProviderIntegratedFunction : StorageIntegratedFunction
    {
        private Func<string, IAuthJanitorProvider> _providerFactory;
        private Func<string, AuthJanitorProviderConfiguration> _providerConfigurationFactory;
        private Func<string, LoadedProviderMetadata> _providerDetailsFactory;

        protected IAuthJanitorProvider GetProvider(string name) => _providerFactory(name);
        protected AuthJanitorProviderConfiguration GetProviderConfiguration(string name) => _providerConfigurationFactory(name);
        protected LoadedProviderMetadata GetProviderDetails(string name) => _providerDetailsFactory(name);
        protected List<LoadedProviderMetadata> LoadedProviders { get; }

        protected ProviderIntegratedFunction(
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Resource, ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>> configViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate,
            Func<string, IAuthJanitorProvider> providerFactory,
            Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory,
            Func<string, LoadedProviderMetadata> providerDetailsFactory,
            List<LoadedProviderMetadata> loadedProviders) : base(managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
            _providerFactory = providerFactory;
            _providerConfigurationFactory = providerConfigurationFactory;
            _providerDetailsFactory = providerDetailsFactory;
            LoadedProviders = loadedProviders;
        }
    }
}
