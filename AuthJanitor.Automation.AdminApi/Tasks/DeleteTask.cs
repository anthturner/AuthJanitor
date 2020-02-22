using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class DeleteTask
    {
        [FunctionName("DeleteTask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/{taskId:guid}/delete")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Deleting Task ID {0}.", taskId);

            CloudBlobDirectory taskStoreDirectory = null;
            IDataStore<RekeyingTask> taskStore = new BlobDataStore<RekeyingTask>(taskStoreDirectory);

            await taskStore.Delete(taskId);
            return new OkResult();
        }
    }
}
