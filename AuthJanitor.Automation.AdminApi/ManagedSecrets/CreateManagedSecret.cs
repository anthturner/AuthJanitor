using AuthJanitor.Automation.AdminApi.ManagedSecrets;
using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi
{
    public static class CreateManagedSecret
    {
        [FunctionName("CreateManagedSecret")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secrets")] ManagedSecret inputSecret,
            HttpRequest req,
            [Blob("authjanitor/secrets", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob secretsBlob,
            [Blob("authjanitor/resources", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            ILogger log)
        {
            log.LogInformation("Creating new Managed Secret");

            IDataStore<ManagedSecret> secretStore = 
                await new BlobDataStore<ManagedSecret>(secretsBlob).Initialize();
            IDataStore<Resource> resourceStore = 
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            var allResources = await resourceStore.List();
            var allResourceIds = allResources.Select(r => r.ObjectId);
            if (inputSecret.ResourceIds.Any(r => !allResourceIds.Contains(r)))
            {
                var invalidIds = inputSecret.ResourceIds.Where(r => !allResourceIds.Contains(r));
                log.LogError("New Managed Secret attempted to link one or more invalid Resource IDs: {0}", invalidIds);
                return new BadRequestErrorMessageResult("One or more ResourceIds not found!");
            }

            ManagedSecret newManagedSecret = new ManagedSecret()
            {
                Name = inputSecret.Name,
                Description = inputSecret.Description,
                ValidPeriod = inputSecret.ValidPeriod,
                ResourceIds = inputSecret.ResourceIds
            };

            await secretStore.Create(newManagedSecret);
            await secretStore.Commit();

            log.LogInformation("Created new Managed Secret '{0}'", newManagedSecret.Name);

            return new OkObjectResult(ManagedSecretViewModel.FromManagedSecret(newManagedSecret, allResources));
        }
    }
}
