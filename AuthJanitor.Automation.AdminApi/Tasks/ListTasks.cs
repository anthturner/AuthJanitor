using AuthJanitor.Automation.AdminApi.Tasks;
using AuthJanitor.Automation.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class ListTasks
    {
        [FunctionName("ListTasks")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/list")] HttpRequest req,
            [Blob("authjanitor/secrets", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob secretsBlob,
            [Blob("authjanitor/tasks", FileAccess.Read, Connection = "AzureWebJobsStorage")] CloudBlockBlob tasksBlob,
            ILogger log)
        {
            log.LogInformation("List all Tasks.");

            var secretStore = await new BlobDataStore<ManagedSecret>(secretsBlob).Initialize();
            var taskStore = await new BlobDataStore<RekeyingTask>(tasksBlob).Initialize();

            var secrets = await secretStore.List();
            var tasks = await taskStore.List();

            return new OkObjectResult(tasks.Select(t => RekeyingTaskViewModel.FromRekeyingTask(t, secrets)));
        }
    }
}
