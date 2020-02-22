using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi
{
    public static class ListManagedSecrets
    {
        [FunctionName("ListManagedSecrets")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/secrets/list")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Listing all Managed Secret IDs.");

            CloudBlobDirectory secretStoreDirectory = null;
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretStoreDirectory);

            return new OkObjectResult(await secretStore.List());
        }
    }
}
