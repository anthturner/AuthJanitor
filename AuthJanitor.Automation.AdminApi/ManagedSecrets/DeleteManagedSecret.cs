using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AuthJanitor.Automation.AdminApi
{
    public static class DeleteManagedSecret
    {
        [FunctionName("DeleteManagedSecret")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/secrets/{secretId:guid}/delete")] HttpRequest req,
            Guid secretId,
            ILogger log)
        {

            log.LogInformation("Deleting Managed Secret {0}", secretId);

            CloudBlobDirectory secretStoreDirectory = null;
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretStoreDirectory);

            await secretStore.Delete(secretId);

            return new OkResult();
        }
    }
}
