using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace AuthJanitor
{
    public static class ApproveTask
    {
        [FunctionName("ApproveTask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/{taskId:guid}/approve")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            // TODO: Header check -- Handle similarly to Azure metadata service

            log.LogInformation("Administrator approved Task ID {0}", taskId);

            CloudBlobDirectory taskStoreDirectory = null;
            CloudBlobDirectory secretsDirectory = null;
            CloudBlobDirectory resourcesDirectory = null;
            IDataStore<RekeyingTask> taskStore = new BlobDataStore<RekeyingTask>(taskStoreDirectory);
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretsDirectory);
            IDataStore<Resource> resourceStore = new BlobDataStore<Resource>(resourcesDirectory);

            var task = await taskStore.Get(taskId);
            var secretResults = new Dictionary<Guid, string>();

            if (task.Expiry < DateTime.Now)
                log.LogError("Drop-dead time has expired; this rekeying operation may be a little bumpy!");

            var allResources = await resourceStore.Get();
            var allResourceIds = allResources.Select(r => r.ObjectId);

            foreach (var managedSecretId in task.ManagedSecretIds)
            {
                try
                {
                    log.LogInformation("Rekeying Managed Secret ID {0}", managedSecretId);
                    var secret = await secretStore.Get(managedSecretId);

                    if (secret.ResourceIds.Any(id => !allResourceIds.Contains(id)))
                        return new BadRequestErrorMessageResult("Invalid Resource ID in set");

                    var resources = secret.ResourceIds.Select(id => resourceStore.Get(id).Result);
                    var providers = resources.Select(r => AuthJanitorProviderFactory.CreateFromResource<IAuthJanitorProvider>(r)).ToArray();
                    await HelperMethods.RunRekeyingWorkflow(secret.ValidPeriod, providers);
                    secretResults.Add(managedSecretId, "Success");
                }
                catch (Exception ex)
                {
                    secretResults.Add(managedSecretId, $"Error: {ex.Message}");
                }
            }            

            return new OkObjectResult(secretResults);
        }
    }
}
