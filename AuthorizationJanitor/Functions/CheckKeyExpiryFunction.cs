using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthorizationJanitor.RotationActions;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AuthorizationJanitor.Functions
{
    public static class CheckKeyExpiryFunction
    {
        private const string TAG_NONCE = "JANITOR_NONCE";
        private const string VAULT_NAME_ENV = "VAULT_NAME";

        private const int RETURN_NO_CHANGE = 0;
        private const int RETURN_CHANGE_OCCURRED = 1;
        private const int RETURN_RETRY_SHORTLY = 2;

        private const int MAX_EXECUTION_SECONDS_BEFORE_RETRY = 300;
        private const int NONCE_LENGTH = 64;

        public static JanitorConfigurationStore ConfigurationStore { get; set; }

        [FunctionName("CheckKeyExpiry")]
        public static async Task<IActionResult> Run(
            [Blob("/authjanitor/")] CloudBlobDirectory cloudBlobDirectory,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/check/{keyName}/{nonce}")] HttpRequest req,
            string appSecretName,
            string nonce,
            ILogger log)
        {
            // todo: check appSecretName/nonce sanitization -- regex to A-Za-z0-9_

            // Populate the wrapper around the configuration storage
            ConfigurationStore = new JanitorConfigurationStore(cloudBlobDirectory);

            // Check if an update is already in progress -- if so, client should call back shortly
            if (await ConfigurationStore.IsLocked(appSecretName))
                return new OkObjectResult(RETURN_RETRY_SHORTLY);

            // Grab the configuration details for the given AppSecret
            var appSecretConfig = await ConfigurationStore.Get(appSecretName);

            // Check key validity based on the nonce -- app should know the nonce from previous retrieval!
            if (appSecretConfig.Nonce.Equals(nonce))
                return new OkObjectResult(RETURN_NO_CHANGE);

            // Check if AppSecret has expired; if not, client nonce is incorrect and CHANGE_OCCURRED is needed.
            //   This should prompt the client to re-request the AppSecret and Nonce from Key Vault, because its copy is out of date.
            if (appSecretConfig.IsValid)
                return new OkObjectResult(RETURN_CHANGE_OCCURRED);

            // Schedule this as long-running to avoid zombie threads after the handler returns (if time expires below)
            var rotationTask = new Task(
                async () => await ExecuteRotation(await ConfigurationStore.Get(appSecretName)), 
                TaskCreationOptions.LongRunning);

            if (!rotationTask.Wait(MAX_EXECUTION_SECONDS_BEFORE_RETRY))
            {
                // Rotation is taking too long for this single request, ask the client to call back shortly
                // This record will be locked until it is complete, so it should continue to return RETRY
                //   until the key has been completely updated by the IRotation instance
                return new OkObjectResult(RETURN_RETRY_SHORTLY);
            }
            else
                return new OkObjectResult(RETURN_CHANGE_OCCURRED);
        }

        private static async Task ExecuteRotation(JanitorConfigurationEntity entity)
        {
            // ConfigurationStore will handle lock/unlock around this Func<Task>
            await ConfigurationStore.PerformLockedTask(entity.FriendlyKeyName, async () =>
            {
                // Get the rotation strategy based on the key type and run it;
                //   the rotation strategy should *block* until it can confirm the key has actually changed
                // The outer handler will take care of sending back a RETRY for long-running tasks
                var replacementEntity = await RotationActionFactory.CreateRotationStrategy(entity.Type).Execute(entity);

                // Regenerate the entity's nonce; if anything below fails, it won't be committed.
                replacementEntity.Nonce = HelperMethods.GenerateCryptographicallySecureString(NONCE_LENGTH);

                // Connect to the Key Vault storing application secrets
                var client = new SecretClient(
                    new Uri($"https://{Environment.GetEnvironmentVariable(VAULT_NAME_ENV)}.vault.azure.net/"), 
                    new DefaultAzureCredential(false));
                var currentSecret = await client.GetSecretAsync(replacementEntity.KeyVaultSecretName);

                // Create a new version of the Secret
                var newSecret = new KeyVaultSecret(replacementEntity.KeyVaultSecretName, replacementEntity.UpdatedKey);
                newSecret.Properties.ContentType = currentSecret.Value.Properties.ContentType;
                newSecret.Properties.NotBefore = DateTimeOffset.Now;
                newSecret.Properties.ExpiresOn = DateTimeOffset.Now + replacementEntity.KeyValidPeriod;
                foreach (var tag in currentSecret.Value.Properties.Tags)
                    newSecret.Properties.Tags.Add(tag.Key, tag.Value);
                newSecret.Properties.Tags[TAG_NONCE] = replacementEntity.Nonce;

                await client.SetSecretAsync(newSecret);

                // Purge key
                replacementEntity.UpdatedKey = string.Empty;

                // Commit
                await ConfigurationStore.Update(replacementEntity);
            });
        }
    }
}
