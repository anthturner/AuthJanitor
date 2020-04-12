using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AuthJanitor.Providers
{

    public class ProviderManagerService
    {
        private readonly IServiceProvider _serviceProvider;

        public ProviderManagerService(
            IServiceProvider serviceProvider,
            params Type[] providerTypes)
        {
            _serviceProvider = serviceProvider;
            LoadedProviders = providerTypes
                .Where(type => !type.IsAbstract && typeof(IAuthJanitorProvider).IsAssignableFrom(type))
                .Select(type => new LoadedProviderMetadata()
                {
                    OriginatingFile = Path.GetFileName(type.Assembly.Location),
                    AssemblyName = type.Assembly.GetName(),
                    ProviderTypeName = type.AssemblyQualifiedName,
                    ProviderType = type,
                    ProviderConfigurationType = type.BaseType.GetGenericArguments()[0],
                    Details = type.GetCustomAttribute<ProviderAttribute>(),
                    SvgImage = type.GetCustomAttribute<ProviderImageAttribute>()?.SvgImage
                })
                .ToList()
                .AsReadOnly();
        }

        public static void ConfigureServices(IServiceCollection serviceCollection, params Type[] loadedProviderTypes)
        {
            serviceCollection.AddSingleton<ProviderManagerService>((s) => new ProviderManagerService(s, loadedProviderTypes));
        }

        public bool HasProvider(string name) => LoadedProviders.Any(t => t.ProviderTypeName == name);
        public LoadedProviderMetadata GetProviderMetadata(string name) => LoadedProviders.FirstOrDefault(t => t.ProviderTypeName == name);

        public IAuthJanitorProvider GetProvider(RekeyingAttemptLogger logger, string name)
        {
            if (!HasProvider(name))
                throw new Exception($"Provider '{name}' not available!");
            else
                return ActivatorUtilities.CreateInstance(_serviceProvider, GetProviderMetadata(name).ProviderType) as IAuthJanitorProvider;
        }

        public IAuthJanitorProvider GetProvider(RekeyingAttemptLogger logger, string name, string configuration)
        {
            var provider = GetProvider(logger, name);
            provider.SerializedConfiguration = configuration;
            return provider;
        }

        public AuthJanitorProviderConfiguration GetProviderConfiguration(string name) => ActivatorUtilities.CreateInstance(_serviceProvider, GetProviderMetadata(name).ProviderConfigurationType) as AuthJanitorProviderConfiguration;
        public AuthJanitorProviderConfiguration GetProviderConfiguration(string name, string serializedConfiguration) => JsonConvert.DeserializeObject(serializedConfiguration, GetProviderMetadata(name).ProviderConfigurationType) as AuthJanitorProviderConfiguration;
        public IReadOnlyList<LoadedProviderMetadata> LoadedProviders { get; }

        public RekeyingAttemptLogger ExecuteRekeyingWorkflow(TimeSpan validPeriod, params IAuthJanitorProvider[] providers)
        {
            var attemptLogger = new RekeyingAttemptLogger();
            ExecuteRekeyingWorkflow(attemptLogger, validPeriod, providers);
            return attemptLogger;
        }

        private void ExecuteRekeyingWorkflow(RekeyingAttemptLogger logger, TimeSpan validPeriod, IEnumerable<IAuthJanitorProvider> providers)
        {
            logger.LogInformation("########## BEGIN REKEYING WORKFLOW ##########");
            var rkoProviders = providers.Where(p => p is IRekeyableObjectProvider).Cast<IRekeyableObjectProvider>();
            var alcProviders = providers.Where(p => p is IApplicationLifecycleProvider).Cast<IApplicationLifecycleProvider>();
            logger.LogInformation("RKO: {0}", string.Join(", ", rkoProviders.Select(p => p.GetType().Name)));
            logger.LogInformation("ALC: {0}", string.Join(", ", alcProviders.Select(p => p.GetType().Name)));

            // -----

            logger.LogInformation("### Performing Provider Tests.");
            var testTasks = providers.Select(p => p.Test().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error running sanity test on provider '{0}'", p.ProviderMetadata.Name);
            }));
            Task.WaitAll(testTasks.ToArray());
            if (testTasks.Any(t => t.IsFaulted))
                throw new Exception("Error running one or more sanity tests!");

            // -----

            logger.LogInformation("### Getting temporary secrets from {0} Rekeyable Object Lifecycle Providers...", rkoProviders.Count());
            var tempSecretTasks = rkoProviders.Select(p => p.GetSecretToUseDuringRekeying().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error getting temporary secret from provider '{0}'", p.ProviderMetadata.Name);
                return t.Result;
            }));
            Task.WaitAll(tempSecretTasks.ToArray());
            if (tempSecretTasks.Any(t => t.IsFaulted))
                throw new Exception("Error retrieving temporary secrets from one or more Rekeyable Object Providers!");

            logger.LogInformation("{0} temporary secrets were created/read to be used during operation.", tempSecretTasks.Count(t => t.Result != null));

            // -----

            logger.LogInformation("### Preparing {0} Application Lifecycle Providers for rekeying...", alcProviders.Count());
            var prepareTasks = alcProviders.Select(p => p.BeforeRekeying(tempSecretTasks.Select(t => t.Result).ToList()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error preparing ALC provider '{0}'", p.GetType().Name);
            }));
            if (prepareTasks.Any(t => t.IsFaulted))
                throw new Exception("Error preparing one or more Application Lifecycle Providers for rekeying!");

            // -----

            logger.LogInformation("### Rekeying {0} Rekeyable Object Providers...", rkoProviders.Count());
            var rekeyingTasks = rkoProviders.Select(p => p.Rekey(validPeriod).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error rekeying provider '{0}'", p.GetType().Name);
                return t.Result;
            }));
            if (rekeyingTasks.Any(t => t.IsFaulted))
                throw new Exception("Error rekeying one or more Rekeyable Object Providers!");

            logger.LogInformation("{0} secrets were regenerated.", rekeyingTasks.Count(t => t.Result != null));

            // -----

            logger.LogInformation("### Committing {0} regenerated secrets to {1} Application Lifecycle Providers...",
                rekeyingTasks.Count(t => t.Result != null),
                alcProviders.Count());
            var commitTasks = alcProviders.Select(p => p.CommitNewSecrets(rekeyingTasks.Select(t => t.Result).ToList()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error committing to provider '{0}'", p.GetType().Name);
            }));
            if (commitTasks.Any(t => t.IsFaulted))
                throw new Exception("Error committing regenerated secrets!");

            // -----

            logger.LogInformation("### Completing post-rekey operations on Application Lifecycle Providers...");
            var postRekeyTasks = alcProviders.Select(p => p.AfterRekeying().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error running post-rekey operations on provider '{0}'", p.GetType().Name);
            }));
            if (postRekeyTasks.Any(t => t.IsFaulted))
                throw new Exception("Error running post-rekey operations on one or more Application Lifecycle Providers!");

            // -----

            logger.LogInformation("### Completing finalizing operations on Rekeyable Object Providers...");
            var afterSwapTasks = rkoProviders.Select(p => p.OnConsumingApplicationSwapped().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error running after-swap operations on provider '{0}'", p.GetType().Name);
            }));
            if (postRekeyTasks.Any(t => t.IsFaulted))
                throw new Exception("Error running after-swap operations on one or more Rekeyable Object Providers!");


            logger.LogInformation("########## END REKEYING WORKFLOW ##########");
        }


        //protected async Task<RekeyingAttemptLogger> ExecuteRekeyingWorkflow(RekeyingTask task, RekeyingAttemptLogger log = null)
        //{
        //    if (log == null) log = new RekeyingAttemptLogger();

        //    MultiCredentialProvider.CredentialType credentialType;
        //    if (task.ConfirmationType == TaskApprovalSources.AdminCachesSignOff)
        //    {
        //        if (task.PersistedCredentialId == default)
        //        {
        //            log.LogError("Cached sign-off is preferred but no credentials were persisted!");
        //            throw new Exception("Cached sign-off is preferred but no credentials were persisted!");
        //        }
        //        var token = await SecureStorageProvider.Retrieve<Azure.Core.AccessToken>(task.PersistedCredentialId);
        //        CredentialProvider.Register(
        //            MultiCredentialProvider.CredentialType.CachedCredential,
        //            token.Token,
        //            token.ExpiresOn);
        //        credentialType = MultiCredentialProvider.CredentialType.CachedCredential;
        //    }
        //    else if (task.ConfirmationType == TaskApprovalSources.AdminSignsOffJustInTime)
        //        credentialType = MultiCredentialProvider.CredentialType.UserCredential;
        //    else
        //        credentialType = MultiCredentialProvider.CredentialType.AgentServicePrincipal;

        //    log.LogInformation("Using credential type {0} to access resources", credentialType);

        //    var secret = await ManagedSecrets.GetAsync(task.ManagedSecretId);
        //    log.LogInformation("Beginning rekeying for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);

        //    var resources = await Resources.GetAsync(r => secret.ResourceIds.Contains(r.ObjectId));
        //    var workflow = new ProviderActionWorkflow(log,
        //        resources.Select(r => GetProvider(log, r.ProviderType, r.ProviderConfiguration, credentialType)));
        //    try
        //    {
        //        await workflow.InvokeAsync(secret.ValidPeriod);
        //        secret.LastChanged = DateTimeOffset.UtcNow;
        //        await ManagedSecrets.UpdateAsync(secret);
        //        log.LogInformation("Completed rekeying workflow for ManagedSecret '{0}' (ID {1})", secret.Name, secret.ObjectId);

        //        if (credentialType == MultiCredentialProvider.CredentialType.CachedCredential)
        //        {
        //            log.LogInformation("Destroying persisted credential");
        //            await SecureStorageProvider.Destroy(task.PersistedCredentialId);
        //        }

        //        log.LogInformation("Rekeying task completed");
        //    }
        //    catch (Exception ex)
        //    {
        //        log.LogCritical(ex, "Error running rekeying task!");
        //        log.LogCritical(ex.Message);
        //        log.LogCritical(ex.StackTrace);
        //        log.OuterException = JsonConvert.SerializeObject(ex, Formatting.Indented);
        //    }

        //    return log;
        //}
    }
}
