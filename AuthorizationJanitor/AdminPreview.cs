using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public static class AdminPreview
    {
        [FunctionName("AdminPreview")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "/admin/preview/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Administrator requested operation preview for Task ID {0}", taskId);

            var taskStore = new QueuedRekeyingTaskStore();
            var secretStore = new ManagedSecretStore();

            var task = await taskStore.GetTask(taskId);
            var results = new List<object>();
            foreach (var managedSecretId in task.ManagedSecretIds)
            {
                var secret = await secretStore.GetManagedSecret(managedSecretId);

                var instance = await secretStore.CreateExtensionFromManagedSecret(log, managedSecretId);
                var description = instance.GetDescription();
                var risks = instance.GetRisks();
                var testResult = await instance.Test();

                results.Add(new
                {
                    managedSecretId,
                    name = secret.Name,
                    expiry = secret.LastChanged + secret.ValidPeriod,
                    description,
                    risks,
                    testResult
                });
            }
            
            return new OkObjectResult(new
            {
                queued = task.Queued,
                dropDead = task.DropDead,
                secrets = results
            });
        }
    }
}
