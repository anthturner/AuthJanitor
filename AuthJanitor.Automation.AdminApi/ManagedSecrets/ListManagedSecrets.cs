using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;

namespace AuthJanitor.Automation.AdminApi
{
    public static class ListManagedSecrets
    {
        [FunctionName("ListManagedSecrets")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/secrets/list")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var secretStore = new ManagedSecretStore();

            return new OkObjectResult(await secretStore.GetManagedSecrets());
        }
    }
}
