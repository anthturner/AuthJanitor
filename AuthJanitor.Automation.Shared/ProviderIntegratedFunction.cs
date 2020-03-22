using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public abstract class ProviderIntegratedFunction : StorageIntegratedFunction
    {
        private readonly Func<string, IAuthJanitorProvider> _providerFactory;
        private readonly Func<string, AuthJanitorProviderConfiguration> _providerConfigurationFactory;
        private readonly Func<string, LoadedProviderMetadata> _providerDetailsFactory;

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
            Func<LoadedProviderMetadata, LoadedProviderViewModel> providerViewModelDelegate,
            Func<string, IAuthJanitorProvider> providerFactory,
            Func<string, AuthJanitorProviderConfiguration> providerConfigurationFactory,
            Func<string, LoadedProviderMetadata> providerDetailsFactory,
            List<LoadedProviderMetadata> loadedProviders) : base(serviceConfiguration, credentialProvider, notificationProvider, secureStorageProvider,  managedSecretStore, resourceStore, rekeyingTaskStore, managedSecretViewModelDelegate, resourceViewModelDelegate, rekeyingTaskViewModelDelegate, configViewModelDelegate, providerViewModelDelegate)
        {
            _providerFactory = providerFactory;
            _providerConfigurationFactory = providerConfigurationFactory;
            _providerDetailsFactory = providerDetailsFactory;
            LoadedProviders = loadedProviders;
        }

        protected async Task<string> ExecuteRekeyingWorkflow(ILogger log, RekeyingTask task)
        {
            MultiCredentialProvider.CredentialType credentialType;
            if (task.ConfirmationType == TaskConfirmationStrategies.AdminCachesSignOff)
            {
                if (task.PersistedCredentialId == default)
                {
                    log.LogError("Cached sign-off is preferred but no credentials were persisted!");
                    return "Cached sign-off is preferred but no credentials were persisted!";
                }
                var token = await SecureStorageProvider.Retrieve<string>(task.PersistedCredentialId);
                CredentialProvider.Register(MultiCredentialProvider.CredentialType.CachedCredential, token);
                credentialType = MultiCredentialProvider.CredentialType.CachedCredential;
            }
            else
                credentialType = MultiCredentialProvider.CredentialType.AgentServicePrincipal;

            log.LogInformation("Using credential type {0} to access resources", credentialType);

            var secrets = task.ManagedSecretIds.Select(s => ManagedSecrets.Get(s));
            foreach (var secret in secrets)
            {
                log.LogInformation("Beginning rekeying for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);

                log.LogDebug("Running access sanity check on {0} Resources associated with ManagedSecret", secret.ResourceIds.Count());
                var testResults = new Dictionary<Guid, bool>();

                // Run all tests in parallel to save time!
                await Task.WhenAll(
                    Resources.Get(r => secret.ResourceIds.Contains(r.ObjectId))
                             .Select(r =>
                                GetProvider(r.ProviderType, r.ProviderConfiguration, credentialType)
                                .Test()
                                .ContinueWith(testTask =>
                                {
                                    testResults[r.ObjectId] = testTask.Result;
                                })));

                if (testResults.Any(r => !r.Value))
                {
                    log.LogCritical("Failed to run sanity checks; skipping this secret!");
                    return "Failed to run sanity checks; skipping this secret!";
                }

                try
                {
                    await HelperMethods.RunRekeyingWorkflow(log, secret.ValidPeriod,
                    Resources.Get(r => secret.ResourceIds.Contains(r.ObjectId))
                             .Select(r => GetProvider(r.ProviderType, r.ProviderConfiguration, credentialType))
                             .ToArray());
                }
                catch (Exception ex)
                {
                    return $"Error ({secret.Name}): {ex.Message}";
                }

                log.LogInformation("Completed rekeying workflow for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);
            }

            if (credentialType == MultiCredentialProvider.CredentialType.CachedCredential)
            {
                log.LogInformation("Destroying persisted credential");
                await SecureStorageProvider.Destroy(task.PersistedCredentialId);
            }

            log.LogInformation("Rekeying task completed");
            return string.Empty;
        }
    }
}
