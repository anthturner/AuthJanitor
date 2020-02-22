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
    public static class ApproveTaskFunction
    {
        [FunctionName("ApproveTask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "/tasks/approve/{taskId:guid}")] HttpRequest req,
            Guid taskId,
            ILogger log)
        {
            // TODO: Header check -- Handle similarly to Azure metadata service

            log.LogInformation("Administrator approved Task ID {0}", taskId);

            var taskStore = new RekeyingTaskStore();
            var secretStore = new ManagedSecretStore();

            var task = await taskStore.GetTask(taskId);
            var secretResults = new Dictionary<Guid, string>();

            if (task.Expiry < DateTime.Now)
                log.LogError("DropDead time has expired; this rekeying operation may be a little bumpy!");


            var resourceStore = new ResourceStore();
            var allResources = resourceStore.GetResources();

            foreach (var managedSecretId in task.ManagedSecretIds)
            {
                try
                {
                    log.LogInformation("Rekeying Managed Secret ID {0}", managedSecretId);
                    var secret = await secretStore.GetManagedSecret(managedSecretId);

                    var resources = new List<Resource>();
                    foreach (var resourceId in secret.ResourceIds)
                    {
                        var resource = allResources.FirstOrDefault(r => r.ResourceId == resourceId);
                        if (resource == null)
                            throw new Exception("Unknown ResourceId in ManagedSecret");
                        resources.Add(resource);
                    }

                    var applicationLifecycleProviders = resources.Where(r => !r.IsRekeyableObjectProvider)
                        .Select(r => AuthJanitorProviderFactory.CreateFromResource<IApplicationLifecycleProvider>(r)).ToList();
                    var rekeyableObjectProviders = resources.Where(r => r.IsRekeyableObjectProvider)
                        .Select(r => AuthJanitorProviderFactory.CreateFromResource<IRekeyableObjectProvider>(r)).ToList();

                    await HelperMethods.RunRekeyingWorkflow(secret.ValidPeriod, applicationLifecycleProviders, rekeyableObjectProviders);
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
