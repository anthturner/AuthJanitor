using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions
{
    public interface IRekeyableServiceExtension : IAuthorizationJanitorExtension
    {
        /// <summary>
        /// Call when ready to rekey a given RekeyableService.
        /// </summary>
        /// <returns></returns>
        Task<RegeneratedKey> Rekey();

        /// <summary>
        /// Call when the ConsumingApplication has been moved to the RegeneratedKey (from Rekey())
        /// </summary>
        /// <returns></returns>
        Task OnConsumingApplicationSwapped();
    }

    /// <summary>
    /// Describes a service which can have its key(s) rotated
    /// </summary>
    public abstract class RekeyableServiceExtension<TConfiguration> : AuthorizationJanitorExtension<TConfiguration>, IRekeyableServiceExtension where TConfiguration : AuthorizationJanitorExtensionConfiguration
    {
        public RekeyableServiceExtension(ILogger logger, TConfiguration configuration) : base(logger, configuration) { }
        public RekeyableServiceExtension(ILogger logger) : base(logger) { }

        /// <summary>
        /// Call when ready to rekey a given RekeyableService.
        /// </summary>
        /// <returns></returns>
        public abstract Task<RegeneratedKey> Rekey();

        /// <summary>
        /// Call when the ConsumingApplication has been moved to the RegeneratedKey (from Rekey())
        /// </summary>
        /// <returns></returns>
        public virtual Task OnConsumingApplicationSwapped()
        {
            return Task.FromResult(true);
        }

        public override string GetDescription()
        {
            return string.IsNullOrEmpty(ResourceName) ? $"+ Rekeyable Service does not have an AzureRM resource associated" :
            $"+ Rekeyable Service has Resource Name '{ResourceName}' from Resource Group '{ResourceGroup}'";
        }
    }
}
