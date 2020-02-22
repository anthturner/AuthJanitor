using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers
{
    public interface IRekeyableObjectProvider : IAuthJanitorProvider
    {
        /// <summary>
        /// Call when ready to rekey a given RekeyableService.
        /// </summary>
        /// <param name="requestedValidPeriod">Requested period of validity for new key/secret</param>
        /// <returns></returns>
        Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod);

        /// <summary>
        /// Call when the ConsumingApplication has been moved to the RegeneratedKey (from Rekey())
        /// </summary>
        /// <returns></returns>
        Task OnConsumingApplicationSwapped();
    }

    /// <summary>
    /// Describes a service which can have its key(s) rotated
    /// </summary>
    public abstract class RekeyableObjectProvider<TConfiguration> : AuthJanitorProvider<TConfiguration>, IRekeyableObjectProvider where TConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Call when ready to rekey a given RekeyableService.
        /// </summary>
        /// <param name="requestedValidPeriod">Requested period of validity for new key/secret</param>
        /// <returns></returns>
        public abstract Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod);

        /// <summary>
        /// Call when the ConsumingApplication has been moved to the RegeneratedKey (from Rekey())
        /// </summary>
        /// <returns></returns>
        public virtual Task OnConsumingApplicationSwapped() => Task.FromResult(true);

        public override string GetDescription()
        {
            return string.IsNullOrEmpty(ResourceName) ? $"+ Rekeyable Service does not have an AzureRM resource associated" :
            $"+ Rekeyable Service has Resource Name '{ResourceName}' from Resource Group '{ResourceGroup}'";
        }
    }
}
