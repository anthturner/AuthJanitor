using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using Microsoft.WindowsAzure.Storage.Blob;

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

            var newResource = new Resource()
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
