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

namespace AuthJanitor.Automation.AdminApi
{
    public static class DeleteManagedSecret
    {
        [FunctionName("DeleteManagedSecret")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "secrets/{secretId:guid}")] HttpRequest req,
            [Blob("authjanitor/secrets", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob secretsBlob,
            Guid secretId,
            ILogger log)
        {
            log.LogInformation("Deleting Managed Secret {0}", secretId);

            IDataStore<ManagedSecret> secretStore =
                await new BlobDataStore<ManagedSecret>(secretsBlob).Initialize();

            await secretStore.Delete(secretId);
            await secretStore.Commit();

            log.LogInformation("Deleted Managed Secret {0}", secretId);

            return new OkResult();
        }
    }
}
