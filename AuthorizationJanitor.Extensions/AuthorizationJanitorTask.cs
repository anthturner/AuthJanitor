using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AuthorizationJanitor.Extensions
{
    /// <summary>
    /// Describes the configuration of a Task
    /// </summary>
    public class AuthorizationJanitorTask
    {
        /// <summary>
        /// Extension Type to instantiate
        /// </summary>
        public Type ConsumingApplicationType { get; set; }

        /// <summary>
        /// Type of RekeyableService to instantiate
        /// </summary>
        public Type RekeyableServiceType { get; set; }

        /// <summary>
        /// Serialized configuration for the ConsumingApplication
        /// </summary>
        public string ConsumingApplicationConfiguration { get; set; }

        /// <summary>
        /// Serialized configuration for the RekeyableService
        /// </summary>
        public string RekeyableServiceConfiguration { get; set; }

        public IConsumingApplicationExtension CreateExtensionInstance(ILogger logger)
        {
            if (ConsumingApplicationType.IsAbstract || !ConsumingApplicationType.IsSubclassOf(typeof(IConsumingApplicationExtension)))
            {
                throw new Exception($"ConsumingApplication type '{ConsumingApplicationType.Name}' is not valid because it is abstract or does not implement IConsumingApplicationExtension");
            }

            if (RekeyableServiceType.IsAbstract || !RekeyableServiceType.IsSubclassOf(typeof(IRekeyableServiceExtension)))
            {
                throw new Exception($"RekeyableService type '{RekeyableServiceType.Name}' is not valid because it is abstract or does not implement IRekeyableServiceExtension");
            }

            object rekeyableConfiguration = JsonConvert.DeserializeObject(RekeyableServiceConfiguration, RekeyableServiceType.GetGenericArguments()[0]);
            object consumerConfiguration = JsonConvert.DeserializeObject(ConsumingApplicationConfiguration, ConsumingApplicationType.GetGenericArguments()[0]);

            object rekeyable = Activator.CreateInstance(RekeyableServiceType, new object[] { logger, rekeyableConfiguration });
            object consumer = Activator.CreateInstance(RekeyableServiceType, new object[] { logger, rekeyable, consumerConfiguration });
            return consumer as IConsumingApplicationExtension;
        }

        public string GetDescription(ILogger logger)
        {
            return CreateExtensionInstance(logger).GetDescription();
        }

        public IList<RiskyConfigurationItem> GetRisks(ILogger logger)
        {
            return CreateExtensionInstance(logger).GetRisks();
        }
    }
}
