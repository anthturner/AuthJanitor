using AuthorizationJanitor.Shared.Configuration;
using CommandLine;
using System;
using System.Threading.Tasks;

namespace AJManager.Verbs
{
    [Verb("list", HelpText = "List configurations for Authorization Janitor instance.")]
    public class ListVerb : IVerb
    {
        [Option("connectionString", HelpText = "Blob Storage connection string")]
        public string ConnectionString { get; set; }

        [Option("containerName", HelpText = "Blob Storage Container name")]
        public string ContainerName { get; set; }

        public async Task Execute()
        {
            var configStore = new AppSecretConfigurationStore(ConnectionString, ContainerName);
            var configurations = await configStore.GetAll();
            foreach (var configuration in configurations)
            {
                Console.WriteLine($"+ {configuration.AppSecretName}");
                Console.WriteLine($"\tType: {configuration.Type.ToString()}");
                Console.WriteLine($"\tValid For: {configuration.AppSecretValidPeriod.ToString()}");
                Console.WriteLine($"\tNext Rotation: {(configuration.LastChanged + configuration.AppSecretValidPeriod).ToString()}");
                Console.WriteLine($"\tBacking Secret: {configuration.KeyVaultSecretName}");
                Console.WriteLine($"\tOptions:");
                Console.WriteLine($"\t\t{configuration.SerializedRotationConfiguration}");
                Console.WriteLine();
            }
        }
    }
}
