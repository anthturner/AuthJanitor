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

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public static class PreviewTaskActions
    {
        [FunctionName("PreviewTaskActions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/{taskId:guid}/preview")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            log.LogInformation("Preview Actions for Task ID {0}.", taskId);

            CloudBlobDirectory taskStoreDirectory = null;
            CloudBlobDirectory resourceStoreDirectory = null;
            CloudBlobDirectory secretStoreDirectory = null;
            IDataStore<RekeyingTask> taskStore = new BlobDataStore<RekeyingTask>(taskStoreDirectory);
            IDataStore<Resource> resourceStore = new BlobDataStore<Resource>(resourceStoreDirectory);
            IDataStore<ManagedSecret> secretStore = new BlobDataStore<ManagedSecret>(secretStoreDirectory);

            IList<Guid> allRegisteredResources = await resourceStore.List();

            RekeyingTask task = await taskStore.Get(taskId);
            List<object> secrets = new List<object>();
            foreach (Guid managedSecretId in task.ManagedSecretIds)
            {
                ManagedSecret managedSecret = await secretStore.Get(managedSecretId);

                if (managedSecret.ResourceIds.Any(id => !allRegisteredResources.Contains(id)))
                {
                    return new BadRequestErrorMessageResult("Unknown Resource ID in Task");
                }

                List<ResourceSummary> resources = new List<ResourceSummary>();
                foreach (Guid resourceId in managedSecret.ResourceIds)
                {
                    Resource resource = await resourceStore.Get(resourceId);
                    IAuthJanitorProvider provider = AuthJanitorProviderFactory.CreateFromResource<IAuthJanitorProvider>(resource);
                    resources.Add(new ResourceSummary(resource)
                    {
                        ActionDescription = provider.GetDescription(),
                        Risks = provider.GetRisks(managedSecret.ValidPeriod),
                        TestResult = await provider.Test()
                    });
                }

                secrets.Add(new
                {
                    ObjectId = managedSecret.ObjectId,
                    name = managedSecret.Name,
                    expiry = managedSecret.LastChanged + managedSecret.ValidPeriod,
                    description = managedSecret.Description,
                    resources = resources
                });
            }

            return new OkObjectResult(new
            {
                queued = task.Queued,
                expiry = task.Expiry,
                secrets = secrets
            });
        }
    }
}
