﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AzureAD
{
    [Provider(Name = "Access Token",
              IconClass = "fa fa-key",
              Description = "Acquires an Access Token from Azure AD with a given set of scopes")]
    [ProviderImage(ProviderImages.AZURE_AD_SVG)]
    public class AccessTokenRekeyableObjectProvider : RekeyableObjectProvider<AccessTokenConfiguration>
    {
        public AccessTokenRekeyableObjectProvider(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory, serviceProvider)
        {
        }

        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            // requestedValidPeriod is ignored here, AAD sets token expiry!

            var token = await _serviceProvider.GetRequiredService<MultiCredentialProvider>()
                .Get(CredentialType)
                .AzureIdentityTokenCredential
                .GetTokenAsync(new Azure.Core.TokenRequestContext(Configuration.Scopes), System.Threading.CancellationToken.None);
            
            return new RegeneratedSecret()
            {
                UserHint = Configuration.UserHint,
                NewSecretValue = token.Token,
                Expiry = token.ExpiresOn
            };
        }
    }
}
