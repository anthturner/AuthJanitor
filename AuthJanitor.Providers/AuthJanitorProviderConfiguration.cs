using System.Collections.Generic;

namespace AuthJanitor.Providers
{
    /// <summary>
    /// Describes the configuration of an Extension
    /// </summary>
    public abstract class AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Resource Group name
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Resource name (inside Resource Group)
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Arbitrary user-specified hint string (from Provider Configuration) used to distinguish among multiple 
        /// RegeneratedSecrets entering an ApplicationLifecycleProvider
        /// </summary>
        public string UserHint { get; set; }

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescriptiveString()
        {
            return $"Resource Group: {ResourceGroup} - Resource Name: {ResourceName}";
        }

        /// <summary>
        /// Get a list of configuration choices that might be risky
        /// </summary>
        /// <returns></returns>
        public virtual IList<RiskyConfigurationItem> GetRiskyConfigurations()
        {
            return new List<RiskyConfigurationItem>();
        }
    }
}
