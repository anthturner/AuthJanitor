using AuthJanitor.Automation.Shared;
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

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class GetResource
    {
        [FunctionName("GetResource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/{resourceId:guid}")] HttpRequest req,
            [Blob("authjanitor/resources", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Get Resource ID {0}.", resourceId);

            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            var resource = await resourceStore.Get(resourceId);
            if (resource == null)
                return new BadRequestErrorMessageResult("Invalid Resource ID!");

            return new OkObjectResult(ResourceViewModel.FromResourceWithConfiguration(resource));
        }
    }
}
