using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using System;

namespace AuthJanitor.Automation.Shared
{
    public abstract class StorageIntegratedFunction
    {
        private readonly Func<ManagedSecret, ManagedSecretViewModel> _managedSecretViewModelDelegate;
        private readonly Func<Resource, ResourceViewModel> _resourceViewModelDelegate;
        private readonly Func<RekeyingTask, RekeyingTaskViewModel> _rekeyingTaskViewModelDelegate;
        private readonly Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> _configViewModelDelegate;
        private readonly Func<ScheduleWindow, ScheduleWindowViewModel> _scheduleViewModelDelegate;
        private readonly Func<LoadedProviderMetadata, LoadedProviderViewModel> _providerViewModelDelegate;

        protected IDataStore<ManagedSecret> ManagedSecrets { get; }
        protected IDataStore<Resource> Resources { get; }
        protected IDataStore<RekeyingTask> RekeyingTasks { get; }

        protected AuthJanitorServiceConfiguration ServiceConfiguration { get; }
        protected EventDispatcherService EventDispatcherService { get; }
        protected MultiCredentialProvider CredentialProvider { get; }
        protected ISecureStorageProvider SecureStorageProvider { get; }

        protected ManagedSecretViewModel GetViewModel(ManagedSecret secret) => _managedSecretViewModelDelegate(secret);
        protected ResourceViewModel GetViewModel(Resource resource) => _resourceViewModelDelegate(resource);
        protected RekeyingTaskViewModel GetViewModel(RekeyingTask rekeyingTask) => _rekeyingTaskViewModelDelegate(rekeyingTask);
        protected ProviderConfigurationViewModel GetViewModel(AuthJanitorProviderConfiguration config) => _configViewModelDelegate(config);
        protected ScheduleWindowViewModel GetViewModel(ScheduleWindow schedule) => _scheduleViewModelDelegate(schedule);
        protected LoadedProviderViewModel GetViewModel(LoadedProviderMetadata provider) => _providerViewModelDelegate(provider);

        protected StorageIntegratedFunction(
            AuthJanitorServiceConfiguration serviceConfiguration,
            MultiCredentialProvider credentialProvider,
            EventDispatcherService eventDispatcherService,
            ISecureStorageProvider secureStorageProvider,
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Resource, ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate,
            Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate)
        {
            ServiceConfiguration = serviceConfiguration;
            CredentialProvider = credentialProvider;
            EventDispatcherService = eventDispatcherService;
            SecureStorageProvider = secureStorageProvider;

            ManagedSecrets = managedSecretStore;
            Resources = resourceStore;
            RekeyingTasks = rekeyingTaskStore;

            _managedSecretViewModelDelegate = managedSecretViewModelDelegate;
            _resourceViewModelDelegate = resourceViewModelDelegate;
            _rekeyingTaskViewModelDelegate = rekeyingTaskViewModelDelegate;
            _configViewModelDelegate = configViewModelDelegate;
            _scheduleViewModelDelegate = scheduleViewModelDelegate;
            _providerViewModelDelegate = providerViewModelDelegate;
        }
    }
}
