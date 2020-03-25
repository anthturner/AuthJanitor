using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuthJanitor.Providers.AzureAD
{
    public class AccessTokenConfiguration : AuthJanitorProviderConfiguration
    {
        /// <summary>
        /// Scopes/Resources to request for the Access Token
        /// </summary>
        [Description("Access Token Scopes")]
        public string[] Scopes { get; set; }
    }
}
