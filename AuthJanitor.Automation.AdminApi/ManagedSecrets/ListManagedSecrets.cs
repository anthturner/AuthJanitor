using AuthJanitor.Automation.AdminApi.ManagedSecrets;
using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi
{
    public static class ListManagedSecrets
    {
        [FunctionName("ListManagedSecrets")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secrets")] HttpRequest req,
            [Blob("authjanitor/secrets", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob secretsBlob,
            [Blob("authjanitor/resources", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            ILogger log)
        {
            log.LogInformation("Listing all Managed Secrets.");

            IDataStore<ManagedSecret> secretStore =
                await new BlobDataStore<ManagedSecret>(secretsBlob).Initialize();
            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            var allResources = await resourceStore.List();

            return new OkObjectResult((await secretStore.List()).Select(s =>
                ManagedSecretViewModel.FromManagedSecret(s, allResources)));
        }
    }
}
