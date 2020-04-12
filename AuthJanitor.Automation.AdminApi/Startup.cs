using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.DataStores;
using AuthJanitor.Automation.Shared.Models;
using AuthJanitor.Automation.Shared.PersistenceEncryption;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Providers;
using McMaster.NETCore.Plugins;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

[assembly: WebJobsStartup(typeof(AuthJanitor.Automation.AdminApi.Startup))]
namespace AuthJanitor.Automation.AdminApi
{
    public class Startup : IWebJobsStartup
    {
        private const string RESOURCES_BLOB_NAME = "resources";
        private const string MANAGED_SECRETS_BLOB_NAME = "secrets";
        private const string REKEYING_TASKS_BLOB_NAME = "tasks";
        private const string SCHEDULES_BLOB_NAME = "schedules";

        private const string PROVIDER_SEARCH_MASK = "AuthJanitor.Providers.*.dll";
        private static readonly string PROVIDER_SEARCH_PATH = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ".."));
        private static readonly Type[] PROVIDER_SHARED_TYPES = new Type[]
        {
            typeof(IAuthJanitorProvider),
            typeof(AuthJanitorProvider<>),
            typeof(IApplicationLifecycleProvider),
            typeof(ApplicationLifecycleProvider<>),
            typeof(IRekeyableObjectProvider),
            typeof(RekeyableObjectProvider<>),
            typeof(IServiceCollection),
            typeof(ILogger)
        };

        public static IServiceProvider ServiceProvider { get; set; }

        private static AuthJanitorServiceConfiguration ServiceConfiguration =>
            new AuthJanitorServiceConfiguration()
            {
                ApprovalRequiredLeadTimeHours = 24 * 7,
                AutomaticRekeyableJustInTimeLeadTimeHours = 24 * 2,
                AutomaticRekeyableTaskCreationLeadTimeHours = 24 * 7,
                ExternalSignalRekeyableLeadTimeHours = 24,
                MetadataStorageContainerName = "authjanitor",
                SecurePersistenceContainerName = "authjanitor",
                SecurePersistenceEncryptionKey = "iamnotastrongkey!"
            };

        public void Configure(IWebJobsBuilder builder)
        {
            var logger = new LoggerFactory().CreateLogger(nameof(Startup));

            logger.LogDebug("Registering LoggerFactory");
            builder.Services.AddSingleton<ILoggerFactory>(new LoggerFactory());

            // TODO: Load this from somewhere?
            logger.LogDebug("Registering Service Configuration");
            builder.Services.AddSingleton(ServiceConfiguration);

            logger.LogDebug("Registering Event Dispatcher");
            // TODO: Register IEventSinks here
            builder.Services.AddSingleton<EventDispatcherService>();
            //builder.Services.AddSingleton<INotificationProvider>(new EmailNotificationProvider(
            //    Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process),
            //    "http://localhost:16000/aj/",
            //    "authjanitor@bitoblivion.com"));

            logger.LogDebug("Registering Secure Storage Provider");
            builder.Services.AddSingleton<ISecureStorageProvider>(s =>
                new KeyVaultSecureStorageProvider(
                    new Rfc2898AesPersistenceEncryption(
                        s.GetRequiredService<AuthJanitorServiceConfiguration>()
                         .SecurePersistenceEncryptionKey),
                    s.GetRequiredService<MultiCredentialProvider>(),
                    s.GetRequiredService<AuthJanitorServiceConfiguration>()
                     .SecurePersistenceContainerName));

            // -----

            logger.LogDebug("Registering DataStores");
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            builder.Services.AddSingleton<IDataStore<ManagedSecret>>(
                new AzureBlobDataStore<ManagedSecret>(
                    connectionString,
                    ServiceConfiguration.MetadataStorageContainerName,
                    MANAGED_SECRETS_BLOB_NAME));
            builder.Services.AddSingleton<IDataStore<RekeyingTask>>(
                new AzureBlobDataStore<RekeyingTask>(
                    connectionString,
                    ServiceConfiguration.MetadataStorageContainerName,
                    REKEYING_TASKS_BLOB_NAME));
            builder.Services.AddSingleton<IDataStore<Resource>>(
                new AzureBlobDataStore<Resource>(
                    connectionString,
                    ServiceConfiguration.MetadataStorageContainerName,
                    RESOURCES_BLOB_NAME));
            builder.Services.AddSingleton<IDataStore<ScheduleWindow>>(
                new AzureBlobDataStore<ScheduleWindow>(
                    connectionString,
                    ServiceConfiguration.MetadataStorageContainerName,
                    SCHEDULES_BLOB_NAME));

            // -----

            logger.LogDebug("Registering ViewModel generators");
            ViewModelFactory.ConfigureServices(builder.Services);

            // -----

            logger.LogDebug("Scanning for Provider modules at {0}\\{1} recursively", PROVIDER_SEARCH_PATH, PROVIDER_SEARCH_MASK);

            var providerTypes = Directory.GetFiles(PROVIDER_SEARCH_PATH, PROVIDER_SEARCH_MASK, new EnumerationOptions() { RecurseSubdirectories = true })
                                         .SelectMany(libraryFile => PluginLoader.CreateFromAssemblyFile(libraryFile, PROVIDER_SHARED_TYPES)
                                                                            .LoadDefaultAssembly()
                                                                            .GetTypes()
                                                                            .Where(type => !type.IsAbstract && typeof(IAuthJanitorProvider).IsAssignableFrom(type)));

            logger.LogInformation("Found {0} providers: {1}", providerTypes.Count(), string.Join("  ", providerTypes.Select(t => t.Name)));
            logger.LogInformation("Registering providers and service principal default credentials");
            ProviderFactory.ConfigureProviderServices(builder.Services, providerTypes);

            // -----

            ServiceProvider = builder.Services.BuildServiceProvider();
        }
    }
}
