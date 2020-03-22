using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Providers
{
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

        public void Register(CredentialType type, string accessToken)
        {
            // TODO: How to convert an access token (or something else) to the types of credentials below?
            Credentials.Add(new MultiCredential()
            {
                Type = type,
                AccessToken = accessToken
            });
        }

        public MultiCredentialProvider()
        {
            Credentials.Add(new MultiCredential()
            {
                Type = CredentialType.AgentServicePrincipal,
                DefaultAzureCredential = new DefaultAzureCredential(false),
                AzureCredentials = SdkContext.AzureCredentialsFactory.FromMSI(
                                    new MSILoginInformation(MSIResourceType.AppService),
                                    AzureEnvironment.AzureGlobalCloud)
            });
        }

        public class MultiCredential
        {
            public CredentialType Type { get; set; }
            public DefaultAzureCredential DefaultAzureCredential { get; set; }
            public AzureCredentials AzureCredentials { get; set; }
            public string AccessToken { get; set; } // todo: generate from credentials? vice versa?
            public DateTimeOffset Expiry { get; set; } // todo: save this
            public string Username { get; set; } // todo: save this
        }
    }
}
