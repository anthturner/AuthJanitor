using System;

namespace AuthorizationJanitor.Extensions
{
    /// <summary>
    /// Describes a newly generated key/connection string
    /// </summary>
    public class RegeneratedKey
    {
        /// <summary>
        /// Newly generated key
        /// </summary>
        public string NewKey { get; set; }

        /// <summary>
        /// Newly generated connection string, if appropriate
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Retrieve either the Connection String, or if that is not specified, the newly generated Key
        /// </summary>
        public string ConnectionStringOrKey => string.IsNullOrEmpty(ConnectionString) ? NewKey : ConnectionString;

        /// <summary>
        /// If the RekeyableService controls the generated key's expiry, it is stored here
        /// </summary>
        public DateTimeOffset Expiry { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// If the RekeyableService controls the expiry of the generated key
        /// </summary>
        public bool ServiceControlsExpiry => Expiry != DateTimeOffset.MinValue;
    }
}
