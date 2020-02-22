using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor
{
    public static class PreviewTaskActionsFunction
    {
        [FunctionName("PreviewTaskActions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/preview/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            // TODO: Header check -- handle like Azure metadata service

            log.LogInformation("Administrator requested operation preview for Task ID {0}", taskId);

            var taskStore = new RekeyingTaskStore();
            var secretStore = new ManagedSecretStore();
            var resourceStore = new ResourceStore();

            var allResources = resourceStore.GetResources();

            var task = await taskStore.GetTask(taskId);
            var results = new List<object>();
            foreach (var managedSecretId in task.ManagedSecretIds)
            {
                var secret = await secretStore.GetManagedSecret(managedSecretId);

                var resources = new List<Resource>();
                foreach (var resourceId in secret.ResourceIds)
                {
                    var resource = allResources.FirstOrDefault(r => r.ResourceId == resourceId);
                    if (resource == null)
                        throw new Exception("Unknown ResourceId in ManagedSecret");
                    resources.Add(resource);
                }

                List<object> resourceSummaries = new List<object>();
                foreach (var resource in resources)
                {
                    var provider = AuthJanitorProviderFactory.CreateFromResource<IAuthJanitorProvider>(resource);
                    resourceSummaries.Add(new
                    {
                        isRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                        description = provider.GetDescription(),
                        risks = provider.GetRisks(secret.ValidPeriod),
                        test = await provider.Test()
                    });
                }

                results.Add(new
                {
                    managedSecretId,
                    name = secret.Name,
                    expiry = secret.LastChanged + secret.ValidPeriod,
                    description = secret.Description,
                    resources = resourceSummaries
                });
            }
            
            return new OkObjectResult(new
            {
                queued = task.Queued,
                expiry = task.Expiry,
                secrets = results
            });
        }
    }
}
