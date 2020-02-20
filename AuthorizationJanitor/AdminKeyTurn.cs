using AuthorizationJanitor.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthorizationJanitor
{
    public static class AdminKeyTurn
    {
        [FunctionName("AdminKeyTurn")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "/admin/keyturn/{managedSecretId}")] HttpRequest req,
            string managedSecretId,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                // Get managedsecret from ID
                // Link OBO credential to Extension

                //foreach...
                ManagedSecret secret = null;
                // .../foreach
                AuthorizationJanitorTask task = JsonConvert.DeserializeObject<AuthorizationJanitorTask>(secret.SerializedTask);
                IConsumingApplicationExtension instance = task.CreateExtensionInstance(log);
                await instance.Rekey();

                // Adopt RETRY strategy from previous incarnation?

                return new OkResult();
            }
            catch (Exception) { return new InternalServerErrorResult(); }
        }
    }
}
