﻿using System;
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
        [Description("AppSetting Name")]
        public string SettingName { get; set; }

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine + $"AppSetting Name: {SettingName}";
        }
    }
}
