using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class ListResources
    {
        [FunctionName("ListResources")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/resources/list")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("List all Resource IDs.");

            CloudBlobDirectory resourceStoreDirectory = null;
            IDataStore<Resource> resourceStore = new BlobDataStore<Resource>(resourceStoreDirectory);

            return new OkObjectResult(await resourceStore.List());
        }
    }
}
