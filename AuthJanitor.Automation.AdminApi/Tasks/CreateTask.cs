using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class CreateTask
    {
        [FunctionName("CreateTask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/create")] HttpRequest req,
            [FromBody] RekeyingTask resource,
            ILogger log)
        {
            log.LogInformation("Creating new Task.");

            CloudBlobDirectory taskStoreDirectory = null;
            CloudBlobDirectory secretsDirectory = null;
            IDataStore<RekeyingTask> taskStore = new BlobDataStore<RekeyingTask>(taskStoreDirectory);
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretsDirectory);

            var secretIds = await secretStore.List();
            if (resource.ManagedSecretIds.Any(id => !secretIds.Contains(id)))
                return new BadRequestErrorMessageResult("Invalid Managed Secret ID in set");

            var newTask = new RekeyingTask()
            {
                Queued = DateTimeOffset.Now,
                Expiry = resource.Expiry,
                ManagedSecretIds = resource.ManagedSecretIds
            };

            await taskStore.Create(newTask);
            return new OkObjectResult(newTask);
        }
    }
}
