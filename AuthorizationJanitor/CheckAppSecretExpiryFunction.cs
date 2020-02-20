//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Azure.Security.KeyVault.Secrets;
//using Azure.Identity;
//using AuthorizationJanitor.Shared.Configuration;
//using AuthorizationJanitor.Shared.RotationStrategies;
//using AuthorizationJanitor.Shared;
//using System.Web.Http;

//namespace AuthorizationJanitor
//{
//    public static class CheckAppSecretExpiryFunction
//    {
//        private const string TAG_NONCE = "JANITOR_NONCE";
//        private const string VAULT_NAME_ENV = "VAULT_NAME";

//        private const int RETURN_NO_CHANGE = 0;
//        private const int RETURN_CHANGE_OCCURRED = 1;
//        private const int RETURN_RETRY_SHORTLY = 2;

//        private const int MAX_EXECUTION_SECONDS_BEFORE_RETRY = 30;
//        private const int NONCE_LENGTH = 64;

//        public static AppSecretConfigurationStore ConfigurationStore { get; set; }

//        [FunctionName("CheckAppSecretExpiry")]
//        public static async Task<IActionResult> Run(
//            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/check/{appSecretName}/{nonce}")] HttpRequest req,
//            string appSecretName,
//            string nonce,
//            ILogger log)
//        {
//            // todo: check appSecretName/nonce sanitization -- regex to A-Za-z0-9_

//            // Populate the wrapper around the configuration storage
//            ConfigurationStore = new AppSecretConfigurationStore(
//                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
//                Environment.GetEnvironmentVariable("AJContainerName"));

//            // Check if an update is already in progress -- if so, client should call back shortly
//            if (await ConfigurationStore.IsLocked(appSecretName))
//                return new OkObjectResult(RETURN_RETRY_SHORTLY);

//            // Grab the configuration details for the given AppSecret
//            var appSecretConfig = await ConfigurationStore.Get(appSecretName);

//            if (appSecretConfig.IsValid)
//            {
//                // Check AppSecret validity based on the nonce -- caller app should know the nonce from the previous Key Vault retrieval!
//                if (appSecretConfig.IsValid && appSecretConfig.Nonce.Equals(nonce))
//                    return new OkObjectResult(RETURN_NO_CHANGE);
//                // If the AppSecret is still valid but the nonce doesn't match, the client's AppSecret has expired!
//                // The client should restart or re-request the AppSecret and Nonce from Key Vault on CHANGE_OCCURRED
//                else if (appSecretConfig.IsValid && !appSecretConfig.Nonce.Equals(nonce))
//                    return new OkObjectResult(RETURN_CHANGE_OCCURRED);
//            }
//            else
//            {
//                // The AppSecret needs to be rotated!
//                // Schedule this Task as long-running to avoid zombie threads after the handler returns (if time expires below)
//                var rotationTask = new Task(
//                    async () => await ExecuteRotation(log, appSecretConfig),
//                    TaskCreationOptions.LongRunning);
//                rotationTask.Start();

//                if (!rotationTask.Wait(MAX_EXECUTION_SECONDS_BEFORE_RETRY))
//                {
//                    // Rotation is taking too long for this single request, ask the client to call back shortly
//                    // This record will be locked until it is complete, so it should continue to return RETRY
//                    //   until the AppSecret has been completely updated by the IRotation instance
//                    return new OkObjectResult(RETURN_RETRY_SHORTLY);
//                }
//                else
//                    return new OkObjectResult(RETURN_CHANGE_OCCURRED);
//            }
//            return new InternalServerErrorResult();
//        }

//        private static async Task ExecuteRotation(ILogger logger, AppSecretConfiguration entity)
//        {
//            // ConfigurationStore will handle lock/unlock around this Func<Task>
//            await ConfigurationStore.PerformLockedTask(entity.AppSecretName, async () =>
//            {
//                // Get the rotation strategy based on the AppSecret type and run it;
//                //   the rotation strategy should *block* until it can confirm the rotation has actually occurred.
//                // The outer handler will take care of continually sending back a RETRY_SHORTLY for long-running Tasks.
//                var rotationStrategy = RotationStrategyFactory.CreateRotationStrategy(logger, entity);
//                await rotationStrategy.Rotate();

//                // Regenerate the entity's nonce; if anything below fails, it won't be committed.
//                entity.Nonce = HelperMethods.GenerateCryptographicallySecureString(NONCE_LENGTH);

//                // Connect to the Key Vault storing application secrets
//                var client = new SecretClient(
//                    new Uri($"https://{Environment.GetEnvironmentVariable(VAULT_NAME_ENV)}.vault.azure.net/"), 
//                    new DefaultAzureCredential(false));
//                var currentSecret = await client.GetSecretAsync(entity.KeyVaultSecretName);

//                // Create a new version of the Secret
//                var newSecret = new KeyVaultSecret(entity.KeyVaultSecretName, entity.UpdatedAppSecret);

//                // Copy in metadata from the old Secret if it existed
//                if (currentSecret != null && currentSecret.Value != null)
//                {
//                    newSecret.Properties.ContentType = currentSecret.Value.Properties.ContentType;
//                    foreach (var tag in currentSecret.Value.Properties.Tags)
//                        newSecret.Properties.Tags.Add(tag.Key, tag.Value);
//                }
                
//                newSecret.Properties.NotBefore = DateTimeOffset.Now;
//                newSecret.Properties.ExpiresOn = DateTimeOffset.Now + entity.AppSecretValidPeriod;
//                newSecret.Properties.Tags[TAG_NONCE] = entity.Nonce;

//                await client.SetSecretAsync(newSecret);

//                // Purge new AppSecret
//                entity.UpdatedAppSecret = string.Empty;

//                // Commit
//                await ConfigurationStore.Update(entity);
//            });
//        }
//    }
//}
