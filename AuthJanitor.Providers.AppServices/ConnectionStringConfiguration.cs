using Microsoft.Azure.Management.AppService.Fluent.Models;
using System.ComponentModel;

namespace AuthJanitor.Providers.AppServices
{
    /// <summary>
    /// Defines the configuration to update a consumed Connection String for an Azure Functions or Azure WebApps application
    /// </summary>
    public class ConnectionStringConfiguration : SlottableProviderConfiguration
    {
        /// <summary>
        /// Connection String name
        /// </summary>
        [Description("Connection String Name")]
        public string ConnectionStringName { get; set; }

        /// <summary>
        /// Connection String type
        /// </summary>
        [Description("Connection String Type")]
        public ConnectionStringType ConnectionStringType { get; set; }
    }
}
