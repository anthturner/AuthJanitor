using Azure.Identity;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class AccessTokenRekeyableService : RekeyableServiceExtension<AccessTokenConfiguration>
    {
        public AccessTokenRekeyableService(ILogger logger, AccessTokenConfiguration configuration) : base(logger, configuration) { }

        public override async Task<RegeneratedKey> Rekey()
        {
            Azure.Core.AccessToken token = await new DefaultAzureCredential(true).GetTokenAsync(new Azure.Core.TokenRequestContext(Configuration.Scopes), System.Threading.CancellationToken.None);
            return new RegeneratedKey()
            {
                NewKey = token.Token,
                Expiry = token.ExpiresOn
            };
        }
    }
}
