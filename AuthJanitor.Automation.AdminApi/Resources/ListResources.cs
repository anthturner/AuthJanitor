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

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class ListResources
    {
        [FunctionName("ListResources")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources")] HttpRequest req,
            [Blob("authjanitor/resources", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            ILogger log)
        {
            log.LogInformation("List all Resource IDs.");

            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            return new OkObjectResult((await resourceStore.List()).Select(r => ResourceViewModel.FromResource(r)));
        }
    }
}
