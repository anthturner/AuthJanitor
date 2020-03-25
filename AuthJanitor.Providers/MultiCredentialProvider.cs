using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthJanitor.Providers
{
    public class ExistingTokenCredential : TokenCredential
    {
        private AccessToken _accessToken;
        public ExistingTokenCredential(string accessToken, DateTimeOffset expiresOn)
        {
            _accessToken = new AccessToken(accessToken, expiresOn);
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return _accessToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(_accessToken);
        }
    }

    public class MultiCredentialProvider
    {
        public enum CredentialType
        {
            UserCredential,
            CachedCredential,
            AgentServicePrincipal
        }

        public List<MultiCredential> Credentials { get; set; } = new List<MultiCredential>();

        public MultiCredential Get(CredentialType type) => Credentials.FirstOrDefault(c => c.Type == type);

        public void Register(CredentialType type, string accessToken, DateTimeOffset expiresOn)
        {
            Credentials.Add(new MultiCredential()
            {
                Type = type,
                Expiry = expiresOn,
                AzureIdentityTokenCredential = new ExistingTokenCredential(accessToken, expiresOn),
                ServiceClientCredentials = new AzureCredentials(
                    new TokenCredentials(accessToken),
                    new TokenCredentials(accessToken),
                    Environment.GetEnvironmentVariable("TENANT_ID", EnvironmentVariableTarget.Process),
                    AzureEnvironment.AzureGlobalCloud),
                AccessToken = accessToken
            });
        }

        public MultiCredentialProvider()
        {
            Credentials.Add(new MultiCredential()
            {
                Type = CredentialType.AgentServicePrincipal,
                AzureIdentityTokenCredential = new DefaultAzureCredential(false),
                ServiceClientCredentials = SdkContext.AzureCredentialsFactory.FromMSI(
                                    new MSILoginInformation(MSIResourceType.AppService),
                                    AzureEnvironment.AzureGlobalCloud)
            });
        }

        public class MultiCredential
        {
            public CredentialType Type { get; set; }
            public TokenCredential AzureIdentityTokenCredential { get; set; }
            public AzureCredentials ServiceClientCredentials { get; set; }
            public string AccessToken { get; set; } // todo: generate from credentials? vice versa?
            public DateTimeOffset Expiry { get; set; } // todo: save this
            public string Username { get; set; } // todo: save this
        }
    }
}
