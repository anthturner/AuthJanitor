using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthJanitor
{
    public static class ListTasksFunction
    {
        [FunctionName("AdminListTasks")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/list")] HttpRequest req,
            ILogger log)
        {
            // TODO: Header check -- handle like Azure metadata service

            log.LogInformation("Administrator requested list of queued Rekeying Tasks");

            var taskStore = new RekeyingTaskStore();
            var tasks = await taskStore.GetTasks();

            return new OkObjectResult(tasks);
        }
    }
}
