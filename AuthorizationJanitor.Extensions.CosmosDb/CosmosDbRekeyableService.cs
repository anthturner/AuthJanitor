﻿using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class CosmosDbRekeyableService : RekeyableServiceExtension<CosmosDbKeyConfiguration>
    {
        private const string PRIMARY_READONLY_KEY = "primaryReadOnly";
        private const string SECONDARY_READONLY_KEY = "secondaryReadOnly";
        private const string PRIMARY_KEY = "primary";
        private const string SECONDARY_KEY = "secondary";

        public CosmosDbRekeyableService(ILogger logger, CosmosDbKeyConfiguration configuration) : base(logger, configuration) { }

        public override async Task<RegeneratedKey> Rekey()
        {
            ICosmosDBAccount cosmosDbAccount = await (await GetAzure()).CosmosDBAccounts.GetByResourceGroupAsync(ResourceGroup, ResourceName);

            await cosmosDbAccount.RegenerateKeyAsync(GetKeyKindString());

            IDatabaseAccountListKeysResult keys = await cosmosDbAccount.ListKeysAsync();
            return new RegeneratedKey()
            {
                NewKey = GetKeyValue(keys)
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