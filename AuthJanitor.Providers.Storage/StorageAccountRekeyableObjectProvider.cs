using AuthJanitor.Providers;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.Storage
{
    [Provider(
        Name = "Storage Account",
        Description = "Rekey a Storage Account",
        Icon = "fas fa-file-alt")]
    public class StorageAccountRekeyableObjectProvider : RekeyableObjectProvider<StorageAccountKeyConfiguration>
    {
        private const string KEY1 = "key1";
        private const string KEY2 = "key2";
        private const string KERB1 = "kerb1";
        private const string KERB2 = "kerb2";

        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            StorageAccountKey newKey = await Regenerate(GetKeyName());

            return new RegeneratedSecret()
            {
                Expiry = DateTimeOffset.Now + requestedValidPeriod,
                UserHint = Configuration.UserHint,
                NewSecretValue = newKey?.Value
            };
        }

        public override async Task OnConsumingApplicationSwapped()
        {
            if (!Configuration.SkipScramblingOtherKey)
            {
                await Regenerate(GetOtherKeyName());
            }
        }

        private async Task<StorageAccountKey> Regenerate(string keyName)
        {
            Microsoft.Azure.Management.Storage.Fluent.IStorageAccount storageAccount = await (await GetAzure()).StorageAccounts.GetByResourceGroupAsync(ResourceGroup, ResourceName);
            System.Collections.Generic.IReadOnlyList<StorageAccountKey> newKeys = await storageAccount.RegenerateKeyAsync(keyName);
            return newKeys.FirstOrDefault(k => k.KeyName == keyName);
        }

        private string GetKeyName()
        {
            switch (Configuration.KeyType)
            {
                case StorageAccountKeyConfiguration.StorageKeyTypes.Key1: return KEY1;
                case StorageAccountKeyConfiguration.StorageKeyTypes.Key2: return KEY2;
                case StorageAccountKeyConfiguration.StorageKeyTypes.Kerb1: return KERB1;
                case StorageAccountKeyConfiguration.StorageKeyTypes.Kerb2: return KERB2;
            }
            throw new System.Exception($"KeyType '{Configuration.KeyType}' not implemented");
        }

        private string GetOtherKeyName()
        {
            switch (Configuration.KeyType)
            {
                case StorageAccountKeyConfiguration.StorageKeyTypes.Key1: return KEY2;
                case StorageAccountKeyConfiguration.StorageKeyTypes.Key2: return KEY1;
                case StorageAccountKeyConfiguration.StorageKeyTypes.Kerb1: return KERB2;
                case StorageAccountKeyConfiguration.StorageKeyTypes.Kerb2: return KERB1;
            }
            throw new System.Exception($"KeyType '{Configuration.KeyType}' not implemented");
        }
    }
}
