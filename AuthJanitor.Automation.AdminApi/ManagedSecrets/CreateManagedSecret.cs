using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthJanitor.Automation.Shared;
using System.Linq;
using System.Web.Http;

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

            var resourceStore = new ResourceStore();
            var allResourceIds = resourceStore.GetResources().Select(r => r.ResourceId);

            if (inputSecret.ResourceIds.Any(r => !allResourceIds.Contains(r)))
                return new BadRequestErrorMessageResult("One or more ResourceIds not found!");

            var newManagedSecret = new ManagedSecret()
            {
                Name = inputSecret.Name,
                Description = inputSecret.Description,
                ValidPeriod = inputSecret.ValidPeriod,
                ResourceIds = inputSecret.ResourceIds
            };

            var secretStore = new ManagedSecretStore();

            // TODO: Actually create secret
            // TODO: Data validation

            return new OkObjectResult(newManagedSecret);
        }
    }
}
