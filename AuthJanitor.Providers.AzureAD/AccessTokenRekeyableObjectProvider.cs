using Azure.Identity;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AzureAD
{
    public class AccessTokenRekeyableObjectProvider : RekeyableObjectProvider<AccessTokenConfiguration>
    {
        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            // requestedValidPeriod is ignored here, AAD sets token expiry!
            Azure.Core.AccessToken token = await new DefaultAzureCredential(true).GetTokenAsync(new Azure.Core.TokenRequestContext(Configuration.Scopes), System.Threading.CancellationToken.None);
            return new RegeneratedSecret()
            {
                UserHint = Configuration.UserHint,
                NewSecretValue = token.Token,
                Expiry = token.ExpiresOn
            };
        }
    }
}
