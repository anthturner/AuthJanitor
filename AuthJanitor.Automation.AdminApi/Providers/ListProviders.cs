using AuthJanitor.Providers;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi.Providers
{
    public static class ListProviders
    {
        [FunctionName("ListProviders")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "providers")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(
                HelperMethods.ProviderTypes.Select(p => new
                {
                    Name = p.Name,
                    Description = p.GetCustomAttribute<ProviderAttribute>()
                }));
        }
    }
}
