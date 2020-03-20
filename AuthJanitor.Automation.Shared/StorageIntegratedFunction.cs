using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public abstract class StorageIntegratedFunction
    {
        private Func<ManagedSecret, ManagedSecretViewModel> _managedSecretViewModelDelegate;
        private Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> _resourceViewModelDelegate;
        private Func<RekeyingTask, RekeyingTaskViewModel> _rekeyingTaskViewModelDelegate;
        private Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> _configViewModelDelegate;
        private Func<LoadedProviderMetadata, LoadedProviderViewModel> _providerViewModelDelegate;

        protected IDataStore<ManagedSecret> ManagedSecrets { get; }
        protected IDataStore<Shared.Resource> Resources { get; }
        protected IDataStore<RekeyingTask> RekeyingTasks { get; }

        protected INotificationProvider NotificationProvider { get; }

        protected ManagedSecretViewModel GetViewModel(ManagedSecret secret) => _managedSecretViewModelDelegate(secret);
        protected Shared.ViewModels.ResourceViewModel GetViewModel(Shared.Resource resource) => _resourceViewModelDelegate(resource);
        protected RekeyingTaskViewModel GetViewModel(RekeyingTask rekeyingTask) => _rekeyingTaskViewModelDelegate(rekeyingTask);
        protected ProviderConfigurationViewModel GetViewModel(AuthJanitorProviderConfiguration config) => _configViewModelDelegate(config);
        protected LoadedProviderViewModel GetViewModel(LoadedProviderMetadata provider) => _providerViewModelDelegate(provider);

        protected StorageIntegratedFunction(
            INotificationProvider notificationProvider,
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Shared.Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Shared.Resource, Shared.ViewModels.ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate)
        {
            NotificationProvider = notificationProvider;

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
