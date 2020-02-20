using System;

namespace AuthorizationJanitor.Extensions.AppServices
{
    /// <summary>
    /// Defines the configuration to update a consumed AppSetting for an Azure Functions or Azure WebApps application
    /// </summary>
    public class AppSettingConfiguration : SlottableExtensionConfiguration
    {
        /// <summary>
        /// AppSetting Name
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine +
$"AppSetting Name: {SettingName}";
        }
    }
}
