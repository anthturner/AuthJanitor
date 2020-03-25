using System;
using System.ComponentModel;

namespace AuthJanitor.Providers.AppServices
{
    /// <summary>
    /// Defines the configuration to update a consumed AppSetting for an Azure Functions or Azure WebApps application
    /// </summary>
    public class AppSettingConfiguration : SlottableProviderConfiguration
    {
        /// <summary>
        /// AppSetting Name
        /// </summary>
        [DisplayName("AppSetting Name")]
        [Description("AppSetting Name")]
        public string SettingName { get; set; }
    }
}
