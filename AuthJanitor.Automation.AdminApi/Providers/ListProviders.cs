using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Providers;
using System.Linq;
using System.Reflection;

namespace AuthJanitor.Automation.AdminApi.Providers
{
    public static class ListProviders
    {
        [FunctionName("ListProviders")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/providers/list")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(HelperMethods.ProviderTypes.Select(provider =>
                Tuple.Create(provider.Name, provider.GetType().GetCustomAttribute<ProviderAttribute>())));
        }
    }
}
