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
    public static class AdminKeyTurn
    {
        [FunctionName("AdminKeyTurn")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "/admin/keyturn/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Administrator approved Task ID {0}", taskId);

            var taskStore = new QueuedRekeyingTaskStore();
            var secretStore = new ManagedSecretStore();

            var task = await taskStore.GetTask(taskId);
            var secretResults = new Dictionary<Guid, string>();

            if (task.DropDead < DateTime.Now)
                log.LogError("DropDead time has expired; this rekeying operation may be a little bumpy!");

            foreach (var managedSecretId in task.ManagedSecretIds)
            {
                log.LogInformation("Rekeying Managed Secret ID {0}", managedSecretId);
                var secret = await secretStore.GetManagedSecret(managedSecretId);
                var instance = await secretStore.CreateExtensionFromManagedSecret(log, managedSecretId);
                if (!await instance.Test())
                    log.LogWarning("Instance test failed, skipping!");

                await instance.Rekey();
                secretResults.Add(managedSecretId, "Success");
            }            

            return new OkObjectResult(secretResults);
        }
    }
}
