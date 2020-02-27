using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class CreateResource
    {
        [FunctionName("CreateResource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources")] Resource resource,
            HttpRequest req,
            [Blob("authjanitor/resources", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob resourcesBlob,
            ILogger log)
        {
            log.LogInformation("Creating new Resource.");

            IDataStore<Resource> resourceStore =
                await new BlobDataStore<Resource>(resourcesBlob).Initialize();

            HelperMethods.InitializeServiceProvider(new LoggerFactory());
            var provider = HelperMethods.GetProvider(resource.ProviderType);
            if (provider == null)
                return new BadRequestErrorMessageResult("Invalid Provider Type");

            Resource newResource = new Resource()
            {
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = provider is IRekeyableObjectProvider,
                ProviderType = resource.ProviderType,
                ProviderConfiguration = resource.ProviderConfiguration
            };

            await resourceStore.Create(newResource);
            await resourceStore.Commit();

            return new OkObjectResult(ResourceViewModel.FromResource(newResource));
        }
    }
}
