using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public abstract class ProviderIntegratedFunction : StorageIntegratedFunction
    {
        private Func<string, IAuthJanitorProvider> _providerFactory;
        private Func<string, AuthJanitorProviderConfiguration> _providerConfigurationFactory;
        private Func<string, LoadedProviderMetadata> _providerDetailsFactory;

        protected IAuthJanitorProvider GetProvider(string name) => _providerFactory(name);
        protected IAuthJanitorProvider GetProvider(string name, string configuration)
        {
            var provider = GetProvider(name);
            if (provider == null) return null;
            provider.SerializedConfiguration = configuration;
            return provider;
        }
        protected IAuthJanitorProvider GetProvider(string name, string configuration, MultiCredentialProvider.CredentialType credentialType)
        {
            var provider = GetProvider(name, configuration);
            provider.CredentialType = credentialType;
            return provider;
        }

        protected AuthJanitorProviderConfiguration GetProviderConfiguration(string name) => _providerConfigurationFactory(name);
        protected LoadedProviderMetadata GetProviderDetails(string name) => _providerDetailsFactory(name);
        protected List<LoadedProviderMetadata> LoadedProviders { get; }

        protected ProviderIntegratedFunction(
            INotificationProvider notificationProvider,
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Shared.Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate,
            Func<string, IAuthJanitorProvider> providerFactory,
            Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory,
            Func<string, LoadedProviderMetadata> providerDetailsFactory,
            List<LoadedProviderMetadata> loadedProviders) : base(notificationProvider, managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
            _providerFactory = providerFactory;
            _providerConfigurationFactory = providerConfigurationFactory;
            _providerDetailsFactory = providerDetailsFactory;
            LoadedProviders = loadedProviders;
        }
    }
}
