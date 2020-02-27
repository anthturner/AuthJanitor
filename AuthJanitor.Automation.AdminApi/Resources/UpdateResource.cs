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

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class UpdateResource
    {
        [FunctionName("UpdateResource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources/{resourceId:guid}")] Resource resource,
            HttpRequest req,
            [Blob("authjanitor/resources", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            Guid resourceId,
            ILogger log)
        {
            log.LogInformation("Updating Resource ID {0}.", resourceId);

            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            Resource newResource = new Resource()
            {
                ObjectId = resourceId,
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                ProviderType = resource.ProviderType,
                ProviderConfiguration = resource.ProviderConfiguration
            };

            await resourceStore.Update(newResource);
            await resourceStore.Commit();

            return new OkObjectResult(ResourceViewModel.FromResource(newResource));
        }
    }
}
