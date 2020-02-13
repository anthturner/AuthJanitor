using AuthorizationJanitor.Shared.Configuration;
using AuthorizationJanitor.Shared.RotationStrategies;
using CommandLine;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AJManager.Verbs
{
    [Verb("create", HelpText = "Create a new managed secret in the Authorization Janitor configuration store.")]
    public class CreateVerb : IVerb
    {
        [Option("name")]
        public string AppSecretName { get; set; }
        [Option("validPeriod")]
        public TimeSpan ValidPeriod { get; set; }
        [Option("type")]
        public AppSecretConfiguration.AppSecretType Type { get; set; }
        [Option("secretName")]
        public string KeyVaultSecretName { get; set; }

        [Option("options")]
        public string TypeOptions { get; set; }
        [Option("regenerate")]
        public bool ForceRegeneration { get; set; }

        public async Task Execute()
        {
            AppSecretConfigurationStore configStore = null;

            var configuration = new AppSecretConfiguration()
            {
                AppSecretName = AppSecretName,
                AppSecretValidPeriod = ValidPeriod,
                Type = Type,
                KeyVaultSecretName = KeyVaultSecretName,
                LastChanged = DateTimeOffset.MinValue
            };

            IRotationConfiguration rotationConfig = null;
            try
            {
                rotationConfig = RotationStrategyFactory.CreateRotationConfiguration(configuration);
                var providedOptions = JsonConvert.DeserializeObject(TypeOptions, rotationConfig.GetType());
            }
            catch (Exception ex)
            {
                Console.WriteLine("##### Error! #####");
                Console.WriteLine(ex);
            }

            var rotationStrategy = RotationStrategyFactory.CreateRotationStrategy(null, configuration);
            var sanityCheck = await rotationStrategy.SanityCheck();
            var initialSecret = await rotationStrategy.CreateInitialData(ForceRegeneration);

            configuration.PutRotationConfiguration(rotationConfig);
            await configStore.Update(configuration);
        }
    }
}
