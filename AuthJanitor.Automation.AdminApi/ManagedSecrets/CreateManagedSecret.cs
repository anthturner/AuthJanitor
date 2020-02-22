using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using System.Linq;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AuthJanitor.Automation.AdminApi
{
    public static class CreateManagedSecret
    {
        [FunctionName("CreateManagedSecret")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/secrets/create")] HttpRequest req,
            [FromBody] ManagedSecret inputSecret,
            ILogger log)
        {
            log.LogInformation("Creating new Managed Secret");

            CloudBlobDirectory secretStoreDirectory = null;
            CloudBlobDirectory resourceStoreDirectory = null;
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretStoreDirectory);
            IDataStore<Resource> resourceStore = new BlobDataStore<Resource>(resourceStoreDirectory);

            var allResourceIds = await resourceStore.List();
            if (inputSecret.ResourceIds.Any(r => !allResourceIds.Contains(r)))
                return new BadRequestErrorMessageResult("One or more ResourceIds not found!");

            var newManagedSecret = new ManagedSecret()
            {
                Name = inputSecret.Name,
                Description = inputSecret.Description,
                ValidPeriod = inputSecret.ValidPeriod,
                ResourceIds = inputSecret.ResourceIds
            };

            await secretStore.Create(newManagedSecret);
            return new OkObjectResult(newManagedSecret);
        }
    }
}
