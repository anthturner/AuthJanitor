using AuthorizationJanitor.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace AuthorizationJanitor
{
    public static class QueueTasksForRekeying
    {
        [FunctionName("QueueTasksForRekeying")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // TODO: Iterate over configuration for managed secrets
            //       Create an AuthorizationJanitorTask for each

            //foreach...
            ManagedSecret secret = null;
            // .../foreach
            AuthorizationJanitorTask task = JsonConvert.DeserializeObject<AuthorizationJanitorTask>(secret.SerializedTask);

            // Put task on some sort of queue to be executed as-available
        }
    }

    public class ManagedSecret
    {
        /// <summary>
        /// Contains all config for consumer and service provider
        /// </summary>
        public string SerializedTask { get; set; }
    }
}
