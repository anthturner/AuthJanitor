using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class DeleteResource
    {
        [FunctionName("DeleteResource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/resources/{resourceId:guid}/delete")] HttpRequest req,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Delete Resource ID {0}.", resourceId);

            CloudBlobDirectory resourceStoreDirectory = null;
            IDataStore<Resource> resourceStore = new BlobDataStore<Resource>(resourceStoreDirectory);

            try { await resourceStore.Delete(resourceId); return new OkResult(); }
            catch (Exception) { return new BadRequestErrorMessageResult("Invalid Resource ID"); }
        }
    }
}
