using AuthJanitor.Providers;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.CosmosDb
{
    public class CosmosDbRekeyableObjectProvider : RekeyableObjectProvider<CosmosDbKeyConfiguration>
    {
        private const string PRIMARY_READONLY_KEY = "primaryReadOnly";
        private const string SECONDARY_READONLY_KEY = "secondaryReadOnly";
        private const string PRIMARY_KEY = "primary";
        private const string SECONDARY_KEY = "secondary";

        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            ICosmosDBAccount cosmosDbAccount = await (await GetAzure()).CosmosDBAccounts.GetByResourceGroupAsync(ResourceGroup, ResourceName);

            await cosmosDbAccount.RegenerateKeyAsync(GetKeyKindString());

            IDatabaseAccountListKeysResult keys = await cosmosDbAccount.ListKeysAsync();
            return new RegeneratedSecret()
            {
                Expiry = DateTimeOffset.Now + requestedValidPeriod,
                UserHint = Configuration.UserHint,
                NewSecretValue = GetKeyValue(keys)
            };
        }

        public override async Task OnConsumingApplicationSwapped()
        {
            if (!Configuration.SkipScramblingOtherKey)
            {
                string keyType = GetInverseKeyKindString();
                ICosmosDBAccount cosmosDbAccount = await (await GetAzure()).CosmosDBAccounts.GetByResourceGroupAsync(ResourceGroup, ResourceName);
                await cosmosDbAccount.RegenerateKeyAsync(keyType);
            }
        }

        private string GetInverseKeyKindString()
        {
            switch (Configuration.KeyKind)
            {
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary:
                    return GetKeyKindString(CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary);
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary:
                    return GetKeyKindString(CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary);
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly:
                    return GetKeyKindString(CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly);
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly:
                    return GetKeyKindString(CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly);
            }
            throw new System.Exception($"KeyKind '{Configuration.KeyKind}' not implemented");
        }

        private string GetKeyKindString()
        {
            return GetKeyKindString(Configuration.KeyKind);
        }

        private string GetKeyKindString(CosmosDbKeyConfiguration.CosmosDbKeyKinds keyKind)
        {
            switch (Configuration.KeyKind)
            {
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary: return PRIMARY_KEY;
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary: return SECONDARY_KEY;
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly: return PRIMARY_READONLY_KEY;
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly: return SECONDARY_READONLY_KEY;
            }
            throw new System.Exception($"KeyKind '{keyKind}' not implemented");
        }

        private string GetKeyValue(IDatabaseAccountListKeysResult keys)
        {
            return GetKeyValue(keys, Configuration.KeyKind);
        }

        private string GetKeyValue(IDatabaseAccountListKeysResult keys, CosmosDbKeyConfiguration.CosmosDbKeyKinds keyKind)
        {
            switch (keyKind)
            {
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary: return keys.PrimaryMasterKey;
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary: return keys.SecondaryMasterKey;
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly: return keys.PrimaryReadonlyMasterKey;
                case CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly: return keys.SecondaryReadonlyMasterKey;
            }
            throw new System.Exception($"KeyKind '{keyKind}' not implemented");
        }
    }
}