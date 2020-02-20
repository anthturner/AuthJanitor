using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions
{
    public interface IAuthorizationJanitorExtension
    {
        /// <summary>
        /// Resource Group name
        /// </summary>
        public string ResourceGroup { get; }

        /// <summary>
        /// Resource name (inside Resource Group)
        /// </summary>
        public string ResourceName { get; }

        /// <summary>
        /// Azure Credentials used for requests
        /// </summary>
        AzureCredentials AzureCredentials { get; set; }

        /// <summary>
        /// Test if the current credentials can execute an Extension 
        /// </summary>
        /// <returns></returns>
        Task<bool> Test();

        /// <summary>
        /// Get a text description of the action which is taken by the Extension
        /// </summary>
        /// <returns></returns>
        string GetDescription();

        /// <summary>
        /// Get a list of risky items for the Extension
        /// </summary>
        /// <returns></returns>
        IList<RiskyConfigurationItem> GetRisks();
    }

    /// <summary>
    /// Describes an AuthJanitor Extension which either rekeys a service (RekeyableService) or consumes a resource to identify to a service (ConsumingApplication)
    /// </summary>
    public abstract class AuthorizationJanitorExtension<TConfiguration> : IAuthorizationJanitorExtension where TConfiguration : AuthorizationJanitorExtensionConfiguration
    {
        /// <summary>
        /// Static credentials used to perform all operations (if provided)
        /// </summary>
        public static AzureCredentials StaticAzureCredentials { get; set; }

        /// <summary>
        /// Configuration for Extension
        /// </summary>
        public TConfiguration Configuration { get; set; }

        /// <summary>
        /// Resource Group name
        /// </summary>
        public string ResourceGroup => Configuration?.ResourceGroup;

        /// <summary>
        /// Resource name (inside Resource Group)
        /// </summary>
        public string ResourceName => Configuration?.ResourceName;

        /// <summary>
        /// Azure Credentials used for requests
        /// </summary>
        public AzureCredentials AzureCredentials { get; set; }

        /// <summary>
        /// Logger implementation
        /// </summary>
        protected ILogger Logger { get; set; }

        protected AuthorizationJanitorExtension(ILogger logger, TConfiguration configuration = null)
        {
            Logger = logger;
            Configuration = configuration;
        }

        /// <summary>
        /// Test if the current credentials can execute an Extension 
        /// </summary>
        /// <returns></returns>
        public virtual Task<bool> Test()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Get a text description of the action which is taken by the Extension
        /// </summary>
        /// <returns></returns>
        public abstract string GetDescription();

        /// <summary>
        /// Get a list of risky items for the Extension
        /// </summary>
        /// <returns></returns>
        public virtual IList<RiskyConfigurationItem> GetRisks()
        {
            return Configuration.GetRiskyConfigurations();
        }

        protected async Task<Microsoft.Azure.Management.Fluent.IAzure> GetAzure(AzureCredentials credentials = null)
        {
            if (credentials == null && StaticAzureCredentials != null)
            {
                credentials = StaticAzureCredentials;
            }

            return await Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(AzureCredentials)
                .WithDefaultSubscriptionAsync();
        }
    }
}
