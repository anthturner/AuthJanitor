using System.ComponentModel;

namespace AuthJanitor.Providers.Storage
{
    public class StorageAccountKeyConfiguration : AuthJanitorProviderConfiguration
    {
        public enum StorageKeyTypes
        {
            Key1,
            Key2,
            Kerb1,
            Kerb2
        }

        /// <summary>
        /// Kind (type) of Storage Key
        /// </summary>
        [Description("Storage Key")]
        public StorageKeyTypes KeyType { get; set; }

        /// <summary>
        /// Skip the process of scrambling the other (non-active) key
        /// </summary>
        [Description("Skip Scrambling Other Key?")]
        public bool SkipScramblingOtherKey { get; set; }
    }
}
