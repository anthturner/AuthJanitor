using System;
using System.Collections.Generic;

namespace AuthorizationJanitor.Extensions
{
    /// <summary>
    /// Describes the configuration of an Extension
    /// </summary>
    public abstract class AuthorizationJanitorExtensionConfiguration
    {
        /// <summary>
        /// Extension Type to instantiate
        /// </summary>
        public Type ExtensionType { get; set; }

        /// <summary>
        /// Type of RekeyableService to rekey
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Resource Group name
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Resource name (inside Resource Group)
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescriptiveString()
        {
            return $"Extension: {ExtensionType.Name} - Resource Group: {ResourceGroup} - Resource Name: {ResourceName}";
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
