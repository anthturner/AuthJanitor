using AuthorizationJanitor.Shared.Configuration;
using CommandLine;
using System.Threading.Tasks;

namespace AJManager.Verbs
{
    [Verb("unmanage", HelpText = "Stop managing a given Application Secret.")]
    public class UnmanageVerb : IVerb
    {
        [Option("connectionString", HelpText = "Blob Storage connection string")]
        public string ConnectionString { get; set; }

        [Option("containerName", HelpText = "Blob Storage Container name")]
        public string ContainerName { get; set; }

        [Option("appSecretName", HelpText = "AppSecret name")]
        public string AppSecretName { get; set; }

        public async Task Execute()
        {
            var configStore = new AppSecretConfigurationStore(ConnectionString, ContainerName);
            await configStore.Delete(AppSecretName);
        }
    }
}
