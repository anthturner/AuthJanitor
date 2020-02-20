using AuthorizationJanitor.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public static class AdminPreview
    {
        [FunctionName("AdminPreview")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "/admin/preview/{managedSecretId}")] HttpRequest req,
            string managedSecretId,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // get managed secret from its ID
            // Link OBO credential to Extension

            //foreach...
            ManagedSecret secret = null;
            // .../foreach
            AuthorizationJanitorTask task = JsonConvert.DeserializeObject<AuthorizationJanitorTask>(secret.SerializedTask);
            IConsumingApplicationExtension instance = task.CreateExtensionInstance(log);
            string description = instance.GetDescription();
            System.Collections.Generic.IList<RiskyConfigurationItem> risks = instance.GetRisks();
            bool testResult = await instance.Test();

            return new OkObjectResult(new
            {
                description,
                risks,
                testResult
            });
        }
    }
}
