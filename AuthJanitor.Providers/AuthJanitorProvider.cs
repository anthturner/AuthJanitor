using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AuthJanitor.Providers
{
    public interface IAuthJanitorProvider
    {
        /// <summary>
        /// Resource Group name
        /// </summary>
        string ResourceGroup { get; }

        /// <summary>
        /// Resource name (inside Resource Group)
        /// </summary>
        string ResourceName { get; }

        /// <summary>
        /// Serialized ProviderConfiguration
        /// </summary>
        string SerializedConfiguration { get; set; }

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
        /// <param name="requestedValidPeriod">Requested period of validity</param>
        /// <returns></returns>
        IList<RiskyConfigurationItem> GetRisks(TimeSpan requestedValidPeriod);

        /// <summary>
        /// Get the Provider's metadata
        /// </summary>
        ProviderAttribute ProviderMetadata => GetType().GetCustomAttribute<ProviderAttribute>();
    }

    /// <summary>
    /// Describes an AuthJanitor Provider which either rekeys a service (RekeyableService) or manages an application lifecycle (ApplicationLifecycle)
    /// </summary>
    public abstract class AuthJanitorProvider<TConfiguration> : IAuthJanitorProvider where TConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Provider Configuration
        /// </summary>
        public TConfiguration Configuration
        {
            get => JsonConvert.DeserializeObject<TConfiguration>(SerializedConfiguration);
            set => SerializedConfiguration = JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Resource Group name
        /// </summary>
        public string ResourceGroup => Configuration?.ResourceGroup;

        /// <summary>
        /// Resource name (inside Resource Group)
        /// </summary>
        public string ResourceName => Configuration?.ResourceName;

        /// <summary>
        /// Serialized ProviderConfiguration
        /// </summary>
        public string SerializedConfiguration { get; set; }

        /// <summary>
        /// Logger implementation
        /// </summary>
        protected ILogger Logger { get; }

        protected IServiceProvider _serviceProvider;
        protected AuthJanitorProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            Logger = loggerFactory.CreateLogger(GetType());
            _serviceProvider = serviceProvider;
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
        /// Get a text description of the action which is taken by the Provider
        /// </summary>
        /// <returns></returns>
        public abstract string GetDescription();

        /// <summary>
        /// Get a list of risky items for the Provider (base returns Configuration's risks)
        /// </summary>
        /// <param name="requestedValidPeriod">Requested period of validity</param>
        /// <returns></returns>
        public virtual IList<RiskyConfigurationItem> GetRisks(TimeSpan requestedValidPeriod)
        {
            return Configuration.GetRiskyConfigurations();
        }

        protected async Task<Microsoft.Azure.Management.Fluent.IAzure> GetAzure()
        {
            return await Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(_serviceProvider.GetService<AzureCredentials>())
                .WithDefaultSubscriptionAsync();
        }
    }
}
