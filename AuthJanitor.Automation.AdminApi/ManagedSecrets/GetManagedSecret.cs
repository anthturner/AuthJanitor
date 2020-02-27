using AuthJanitor.Automation.AdminApi.ManagedSecrets;
using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi
{
    public static class GetManagedSecret
    {
        [FunctionName("GetManagedSecret")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secrets/{secretId:guid}")] HttpRequest req,
            [Blob("authjanitor/secrets", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob secretsBlob,
            [Blob("authjanitor/resources", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            Guid secretId,
            ILogger log)
        {
            log.LogInformation("Retrieving Managed Secret {0}.", secretId);

            IDataStore<ManagedSecret> secretStore =
                await new BlobDataStore<ManagedSecret>(secretsBlob).Initialize();
            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            var secret = await secretStore.Get(secretId);
            if (secret == null)
                return new BadRequestErrorMessageResult("Secret not found!");

            var allResources = await resourceStore.List();

            return new OkObjectResult(ManagedSecretViewModel.FromManagedSecret(secret, allResources));
        }
    }
}
