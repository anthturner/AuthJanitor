using AuthorizationJanitor.Shared.Configuration;
using CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AJManager.Verbs
{
    [Verb("rotate", HelpText = "Rotate an Application Secret, even if it is not due for rotation.")]
    public class RotateVerb : IVerb
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

            if (await configStore.IsLocked(AppSecretName))
            {
                Console.WriteLine("AppSecret '{0}' currently being rotated!", AppSecretName);
                return;
            }

            Console.WriteLine("Resetting LastChanged to minimum value to force restart...");
            await configStore.PerformLockedTask(AppSecretName, async () =>
            {
                var config = await configStore.Get(AppSecretName);
                config.LastChanged = DateTime.MinValue;
                await configStore.Update(config);
            });
            Console.WriteLine("LastChanged updated! Waiting for application to check in...");
            bool secretWasRelocked = false;
            var startWait = DateTime.Now;
            while (DateTime.Now < startWait + TimeSpan.FromMinutes(5))
            {
                if (DateTime.Now - startWait > TimeSpan.FromSeconds(30))
                    Console.WriteLine("... still waiting ({0}) ...", DateTime.Now - startWait);

                if (!secretWasRelocked && await configStore.IsLocked(AppSecretName))
                {
                    Console.WriteLine("AppSecret has been re-locked; rotation is currently in progress!");
                    secretWasRelocked = true;
                }
                else
                {
                    var config = await configStore.Get(AppSecretName);
                    if (config.IsValid)
                    {
                        Console.WriteLine("AppSecret '{0}' has been successfully rotated!", AppSecretName);
                        break;
                    }
                }

                Thread.Sleep(10000);
            }
        }
    }
}
