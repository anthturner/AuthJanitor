using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor
{
    public static class DeleteTaskFunction
    {
        [FunctionName("DeleteTask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/delete/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            // TODO: Header check -- Handle similarly to Azure metadata service

            log.LogInformation("Administrator deleeted Task ID {0}", taskId);

            var taskStore = new RekeyingTaskStore();
            var secretStore = new ManagedSecretStore();

            var task = await taskStore.GetTask(taskId);

            // TODO: actually delete task!

            return new OkResult();
        }
    }
}
