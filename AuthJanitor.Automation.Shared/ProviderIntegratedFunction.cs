using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public abstract class ProviderIntegratedFunction : StorageIntegratedFunction
    {
        private readonly Func<string, RekeyingAttemptLogger, IAuthJanitorProvider> _providerFactory;
        private readonly Func<string, AuthJanitorProviderConfiguration> _providerConfigurationFactory;
        private readonly Func<string, LoadedProviderMetadata> _providerDetailsFactory;

        protected IAuthJanitorProvider GetProvider(RekeyingAttemptLogger logger, string name) => _providerFactory(name, logger);
        protected IAuthJanitorProvider GetProvider(RekeyingAttemptLogger logger, string name, string configuration)
        {
            var provider = GetProvider(logger, name);
            if (provider == null) return null;
            provider.SerializedConfiguration = configuration;
            return provider;
        }
        protected IAuthJanitorProvider GetProvider(RekeyingAttemptLogger logger, string name, string configuration, MultiCredentialProvider.CredentialType credentialType)
        {
            var provider = GetProvider(logger, name, configuration);
            provider.CredentialType = credentialType;
            return provider;
        }

        protected AuthJanitorProviderConfiguration GetProviderConfiguration(string name) => _providerConfigurationFactory(name);
        protected LoadedProviderMetadata GetProviderDetails(string name) => _providerDetailsFactory(name);
        protected List<LoadedProviderMetadata> LoadedProviders { get; }

        protected ProviderIntegratedFunction(
            AuthJanitorServiceConfiguration serviceConfiguration,
            MultiCredentialProvider credentialProvider,
            INotificationProvider notificationProvider,
            ISecureStorageProvider secureStorageProvider,
            IDataStore<ManagedSecret> managedSecretStore,
            IDataStore<Resource> resourceStore,
            IDataStore<RekeyingTask> rekeyingTaskStore,
            Func<ManagedSecret, ManagedSecretViewModel> managedSecretViewModelDelegate,
            Func<Resource, ResourceViewModel> resourceViewModelDelegate,
            Func<RekeyingTask, RekeyingTaskViewModel> rekeyingTaskViewModelDelegate,
            Func<AuthJanitorProviderConfiguration, ProviderConfigurationViewModel> configViewModelDelegate,
            Func<ScheduleWindow, ScheduleWindowViewModel> scheduleViewModelDelegate,
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate,
            Func<string, RekeyingAttemptLogger, IAuthJanitorProvider> providerFactory,
            Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory,
            Func<string, LoadedProviderMetadata> providerDetailsFactory,
            List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, notificationProvider, secureStorageProvider,  managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, scheduleViewModelDelegate, providerViewModelDelegate)
        {
            _providerFactory = providerFactory;
            _providerConfigurationFactory = providerConfigurationFactory;
            _providerDetailsFactory = providerDetailsFactory;
            LoadedProviders = loadedProviders;
        }

        protected async Task<RekeyingAttemptLogger> ExecuteRekeyingWorkflow(RekeyingTask task, RekeyingAttemptLogger log = null)
        {
            if (log == null) log = new RekeyingAttemptLogger();

            MultiCredentialProvider.CredentialType credentialType;
            if (task.ConfirmationType == TaskConfirmationStrategies.AdminCachesSignOff)
            {
                if (task.PersistedCredentialId == default)
                {
                    log.LogError("Cached sign-off is preferred but no credentials were persisted!");
                    throw new Exception("Cached sign-off is preferred but no credentials were persisted!");
                }
                var token = await SecureStorageProvider.Retrieve<Azure.Core.AccessToken>(task.PersistedCredentialId);
                CredentialProvider.Register(
                    MultiCredentialProvider.CredentialType.CachedCredential, 
                    token.Token,
                    token.ExpiresOn);
                credentialType = MultiCredentialProvider.CredentialType.CachedCredential;
            }
            else if (task.ConfirmationType == TaskConfirmationStrategies.AdminSignsOffJustInTime)
                credentialType = MultiCredentialProvider.CredentialType.UserCredential;
            else
                credentialType = MultiCredentialProvider.CredentialType.AgentServicePrincipal;

            log.LogInformation("Using credential type {0} to access resources", credentialType);

            var secret = await ManagedSecrets.GetAsync(task.ManagedSecretId);
            log.LogInformation("Beginning rekeying for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);

            var resources = await Resources.GetAsync(r => secret.ResourceIds.Contains(r.ObjectId));
            var workflow = new ProviderActionWorkflow(log,
                resources.Select(r => GetProvider(log, r.ProviderType, r.ProviderConfiguration, credentialType)));
            try
            {
                await workflow.InvokeAsync(secret.ValidPeriod);
                secret.LastChanged = DateTimeOffset.UtcNow;
                await ManagedSecrets.UpdateAsync(secret);
                log.LogInformation("Completed rekeying workflow for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);
                
                if (credentialType == MultiCredentialProvider.CredentialType.CachedCredential)
                {
                    log.LogInformation("Destroying persisted credential");
                    await SecureStorageProvider.Destroy(task.PersistedCredentialId);
                }

                log.LogInformation("Rekeying task completed");
            }
            catch (Exception ex)
            {
                log.LogCritical(ex, "Error running rekeying task!");
                log.LogCritical(ex.Message);
                log.LogCritical(ex.StackTrace);
                log.OuterException = JsonConvert.SerializeObject(ex, Formatting.Indented);
            }

            return log;
        }
    }
}
