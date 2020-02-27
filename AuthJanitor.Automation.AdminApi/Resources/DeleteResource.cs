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
    public static class DeleteResource
    {
        [FunctionName("DeleteResource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId:guid}")] HttpRequest req,
            [Blob("authjanitor/resources", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Deleting Resource ID {0}.", resourceId);

            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            try
            { 
                await resourceStore.Delete(resourceId);
                await resourceStore.Commit();
                return new OkResult();
            }
            catch (Exception) { return new BadRequestErrorMessageResult("Invalid Resource ID"); }
        }
    }
}
