using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.CosmosDb
{
    [Provider(Name = "CosmosDB Master Key",
              IconClass = "fa fa-database",
              Description = "Regenerates a Master Key for an Azure CosmosDB instance")]
    [ProviderImage(ProviderImages.COSMOS_DB_SVG)]
    public class CosmosDbRekeyableObjectProvider : RekeyableObjectProvider<CosmosDbKeyConfiguration>
    {
        private const string PRIMARY_READONLY_KEY = "primaryReadOnly";
        private const string SECONDARY_READONLY_KEY = "secondaryReadOnly";
        private const string PRIMARY_KEY = "primary";
        private const string SECONDARY_KEY = "secondary";

        public CosmosDbRekeyableObjectProvider(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public override async Task<RegeneratedSecret> GetSecretToUseDuringRekeying()
        {
            var cosmosDbAccount = await CosmosDbAccount;
            IDatabaseAccountListKeysResult keys = await cosmosDbAccount.ListKeysAsync();
            return new RegeneratedSecret()
            {
                Expiry = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10),
                UserHint = Configuration.UserHint,
                NewSecretValue = GetKeyValue(keys, GetOtherKeyKind(Configuration.KeyKind))
            };
        }

        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            var cosmosDbAccount = await CosmosDbAccount;
            await cosmosDbAccount.RegenerateKeyAsync(GetKeyKindString(Configuration.KeyKind));

            IDatabaseAccountListKeysResult keys = await cosmosDbAccount.ListKeysAsync();
            return new RegeneratedSecret()
            {
                Expiry = DateTimeOffset.UtcNow + requestedValidPeriod,
                UserHint = Configuration.UserHint,
                NewSecretValue = GetKeyValue(keys, Configuration.KeyKind)
            };
        }

        public override async Task OnConsumingApplicationSwapped()
        {
            if (!Configuration.SkipScramblingOtherKey)
            {
                await (await CosmosDbAccount).RegenerateKeyAsync(
                    GetKeyKindString(GetOtherKeyKind(Configuration.KeyKind)));
            }
        }

        public override IList<RiskyConfigurationItem> GetRisks()
        {
            List<RiskyConfigurationItem> issues = new List<RiskyConfigurationItem>();
            if (Configuration.SkipScramblingOtherKey)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 80,
                    Risk = $"The other (unused) CosmosDb Key of this type is not being scrambled during key rotation",
                    Recommendation = "Unless other services use the alternate key, consider allowing the scrambling of the unused key to 'fully' rekey CosmosDb and maintain a high degree of security."
                });
            }

            return issues;
        }

        public override string GetDescription() =>
            $"Regenerates the {Configuration.KeyKind} key for a CosmosDB instance " +
            $"called '{ResourceName}' (Resource Group '{ResourceGroup}'). " +
            $"The {GetOtherKeyKind(Configuration.KeyKind)} key is used as a temporary " +
            $"key while rekeying is taking place. The {GetOtherKeyKind(Configuration.KeyKind)} " +
            $"key will {(Configuration.SkipScramblingOtherKey ? "not" : "also")} be rotated.";

        private Task<ICosmosDBAccount> CosmosDbAccount => GetAzure().ContinueWith(az => az.Result.CosmosDBAccounts.GetByResourceGroupAsync(ResourceGroup, ResourceName)).Unwrap();

        private string GetKeyKindString(CosmosDbKeyConfiguration.CosmosDbKeyKinds keyKind) => keyKind switch
        {
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary => PRIMARY_KEY,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary => SECONDARY_KEY,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly => PRIMARY_READONLY_KEY,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly => SECONDARY_READONLY_KEY,
            _ => throw new System.Exception($"KeyKind '{keyKind}' not implemented")
        };

        private CosmosDbKeyConfiguration.CosmosDbKeyKinds GetOtherKeyKind(CosmosDbKeyConfiguration.CosmosDbKeyKinds keyKind) => keyKind switch
        {
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary => CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary => CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly => CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly => CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly,
            _ => throw new System.Exception($"KeyKind '{keyKind}' not implemented")
        };

        private string GetKeyValue(IDatabaseAccountListKeysResult keys, CosmosDbKeyConfiguration.CosmosDbKeyKinds keyKind) => keyKind switch
        {
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.Primary => keys.PrimaryMasterKey,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.Secondary => keys.SecondaryMasterKey,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.PrimaryReadOnly => keys.PrimaryReadonlyMasterKey,
            CosmosDbKeyConfiguration.CosmosDbKeyKinds.SecondaryReadOnly => keys.SecondaryReadonlyMasterKey,
            _ => throw new System.Exception($"KeyKind '{keyKind}' not implemented")
        };
    }
}