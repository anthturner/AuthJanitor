using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public static class AdminListTasks
    {
        [FunctionName("AdminListTasks")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "/admin/list")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Administrator requested list of queued Rekeying Tasks");

            var taskStore = new QueuedRekeyingTaskStore();
            var tasks = await taskStore.GetTasks();

            return new OkObjectResult(tasks);
        }
    }
}
