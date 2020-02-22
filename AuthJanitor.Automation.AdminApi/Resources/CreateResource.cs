using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class CreateResource
    {
        [FunctionName("CreateResource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/resources/create")] HttpRequest req,
            [FromBody] Resource resource,
            ILogger log)
        {
            log.LogInformation("Creating new Resource.");

            CloudBlobDirectory resourceStoreDirectory = null;
            IDataStore<Resource> resourceStore = new BlobDataStore<Resource>(resourceStoreDirectory);

            Resource newResource = new Resource()
            {
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                ProviderName = resource.ProviderName,
                ProviderConfiguration = resource.ProviderConfiguration
            };

            await resourceStore.Create(newResource);
            return new OkObjectResult(newResource);
        }
    }
}
