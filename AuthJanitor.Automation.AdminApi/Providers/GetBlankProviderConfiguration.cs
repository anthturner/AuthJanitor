using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor.Automation.AdminApi.Providers
{
    public static class GetBlankProviderConfiguration
    {
        [FunctionName("GetBlankProviderConfiguration")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/providers/{providerType}/configuration")] HttpRequest req,
            string providerType,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            AuthJanitor.Providers.AuthJanitorProviderConfiguration configuration = AuthJanitorProviderFactory.CreateProviderConfiguration(providerType);
            if (configuration == null)
            {
                return new BadRequestErrorMessageResult("Invalid Provider Type");
            }

            return new OkObjectResult(configuration);
        }
    }
}
