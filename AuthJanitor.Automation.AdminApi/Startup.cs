using AuthJanitor.Automation.Shared;
using AuthJanitor.Automation.Shared.NotificationProviders;
using AuthJanitor.Automation.Shared.SecureStorageProviders;
using AuthJanitor.Providers;
using McMaster.NETCore.Plugins;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

[assembly: WebJobsStartup(typeof(AuthJanitor.Automation.AdminApi.Startup))]
namespace AuthJanitor.Automation.AdminApi
{
    public class Startup : IWebJobsStartup
    {
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

        public void Configure(IWebJobsBuilder builder)
        {
            var logger = new LoggerFactory().CreateLogger(nameof(Startup));

            logger.LogDebug("Registering LoggerFactory");
            builder.Services.AddSingleton<ILoggerFactory>(new LoggerFactory());

            logger.LogDebug("Registering Notification Provider");
            builder.Services.AddSingleton<INotificationProvider>(new EmailNotificationProvider(
                "sendGridApiKey",
                "uiNotificationUrlBase",
                "authjanitor@bitoblivion.com"));

            logger.LogDebug("Registering Persistence Encryption");
            builder.Services.AddSingleton<IPersistenceEncryption>(
                new Rfc2898AesPersistenceEncryption(Environment.GetEnvironmentVariable("AUTHJANITOR_PERSISTENCE_KEY")));

            logger.LogDebug("Registering Secure Storage Provider");
            builder.Services.AddSingleton<ISecureStorageProvider>((s) =>
                new KeyVaultSecureStorageProvider(
                    s.GetService<IPersistenceEncryption>(),
                    s.GetService<MultiCredentialProvider>(),
                    Environment.GetEnvironmentVariable("AUTHJANITOR_PERSISTENCE_VAULT")));

            logger.LogDebug("Registering DataStores");
            ConfigureStorage(builder.Services).Wait();

            logger.LogDebug("Registering ViewModel generators");
            ViewModelFactory.ConfigureServices(builder.Services);

            logger.LogDebug("Scanning for Provider modules at {0}\\{1} recursively", PROVIDER_SEARCH_PATH, PROVIDER_SEARCH_MASK);
            
            var providerTypes = Directory.GetFiles(PROVIDER_SEARCH_PATH, PROVIDER_SEARCH_MASK, new EnumerationOptions() { RecurseSubdirectories = true })
                                         .SelectMany(libraryFile => PluginLoader.CreateFromAssemblyFile(libraryFile, PROVIDER_SHARED_TYPES)
                                                                            .LoadDefaultAssembly()
                                                                            .GetTypes()
                                                                            .Where(type => !type.IsAbstract && typeof(IAuthJanitorProvider).IsAssignableFrom(type)));

            logger.LogInformation("Found {0} providers: {1}", providerTypes.Count(), string.Join("  ", providerTypes.Select(t => t.Name)));
            logger.LogInformation("Registering providers and service principal default credentials");
            ProviderFactory.ConfigureProviderServices(builder.Services, providerTypes);
            
            ServiceProvider = builder.Services.BuildServiceProvider();
        }

        public static async Task ConfigureStorage(IServiceCollection serviceCollection)
        {
            var secretStore = await GetDataStore<ManagedSecret>("secrets").InitializeAsync();
            var taskStore = await GetDataStore<RekeyingTask>("tasks").InitializeAsync();
            var resourceStore = await GetDataStore<Shared.Resource>("resources").InitializeAsync();

            serviceCollection.AddScoped<IDataStore<ManagedSecret>>((s) => secretStore);
            serviceCollection.AddScoped<IDataStore<RekeyingTask>>((s) => taskStore);
            serviceCollection.AddScoped<IDataStore<Shared.Resource>>((s) => resourceStore);
        }

        private static AzureBlobDataStore<T> GetDataStore<T>(string name) where T : IDataStoreCompatibleStructure
        {
            return new AzureBlobDataStore<T>(CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process))
                .CreateCloudBlobClient()
                .GetContainerReference(Environment.GetEnvironmentVariable("AuthJanitorContainer", EnvironmentVariableTarget.Process))
                .GetBlockBlobReference(name));
        }
    }
}
