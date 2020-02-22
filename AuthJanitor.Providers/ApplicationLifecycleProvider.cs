using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthJanitor.Providers
{
    /// <summary>
    /// Describes an Application Lifecycle Provider which consumes some piece of information to use a Rekeyable Object
    /// </summary>
    public interface IApplicationLifecycleProvider : IAuthJanitorProvider
    {
        /// <summary>
        /// Call to prepare the application for a new secret
        /// </summary>
        Task BeforeRekeying();

        /// <summary>
        /// Call to commit the newly generated secret(s)
        /// </summary>
        Task CommitNewSecrets(List<RegeneratedSecret> newSecrets);

        /// <summary>
        /// Call after all new keys have been committed
        /// </summary>
        Task AfterRekeying();
    }

    /// <summary>
    /// Describes an Application Lifecycle Provider which consumes some piece of information to use a Rekeyable Object
    /// </summary>
    public abstract class ApplicationLifecycleProvider<TProviderConfiguration> : AuthJanitorProvider<TProviderConfiguration>, IApplicationLifecycleProvider
        where TProviderConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Describes an Application Lifecycle Provider which consumes some piece of information to use a Rekeyable Object
        /// </summary>
        protected ApplicationLifecycleProvider() : base() { }

        /// <summary>
        /// Call to prepare the application for a new secret
        /// </summary>
        public virtual Task BeforeRekeying()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Call to commit the newly generated secret(s)
        /// </summary>
        public abstract Task CommitNewSecrets(List<RegeneratedSecret> newSecrets);

        /// <summary>
        /// Call after all new keys have been committed
        /// </summary>
        public virtual Task AfterRekeying()
        {
            return Task.FromResult(true);
        }

        public override string GetDescription()
        {
            return string.IsNullOrEmpty(ResourceName) ? $"+ Consuming Application does not have an AzureRM resource associated" :
            $"+ Consuming Application has Resource Name '{ResourceName}' from Resource Group '{ResourceGroup}'";
        }
    }
}
