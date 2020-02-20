using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions
{
    public interface IConsumingApplicationExtension : IAuthorizationJanitorExtension
    {
        /// <summary>
        /// Call to begin the rekeying process for a given ConsumingApplication
        /// </summary>
        /// <returns></returns>
        Task Rekey();
    }

    /// <summary>
    /// Describes an Extension which consumes some piece of information to connect to a RekeyableService
    /// </summary>
    public abstract class ConsumingApplicationExtension<TConsumerConfiguration> : AuthorizationJanitorExtension<TConsumerConfiguration>, IConsumingApplicationExtension
        where TConsumerConfiguration : AuthorizationJanitorExtensionConfiguration
    {
        /// <summary>
        /// Service being rekeyed
        /// </summary>
        public IRekeyableServiceExtension Service { get; set; }

        public ConsumingApplicationExtension(ILogger logger, IRekeyableServiceExtension service, TConsumerConfiguration configuration) : base(logger, configuration) { Service = service; }
        public ConsumingApplicationExtension(ILogger logger, IRekeyableServiceExtension service) : base(logger) { Service = service; }

        /// <summary>
        /// Call to begin the rekeying process for a given ConsumingApplication
        /// </summary>
        /// <returns></returns>
        public abstract Task Rekey();

        public override string GetDescription()
        {
            return string.IsNullOrEmpty(ResourceName) ? $"+ Consuming Application does not have an AzureRM resource associated" :
            $"+ Consuming Application has Resource Name '{ResourceName}' from Resource Group '{ResourceGroup}'";
        }
    }
}
