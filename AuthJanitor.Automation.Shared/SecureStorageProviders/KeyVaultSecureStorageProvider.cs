using AuthJanitor.Providers;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.SecureStorageProviders
{
    public class KeyVaultSecureStorageProvider : ISecureStorageProvider
    {
        private const string PERSISTENCE_PREFIX = "AJPersist-";
        private string _vaultName;

        private MultiCredentialProvider _credentialProvider;
        public KeyVaultSecureStorageProvider(
            IPersistenceEncryption encryption,
            MultiCredentialProvider credentialProvider,
            string vaultName)
        {
            _credentialProvider = credentialProvider;
            _vaultName = vaultName;
        }

        public async Task DestroyString(Guid persistenceId)
        {
            await GetClient().StartDeleteSecretAsync($"{PERSISTENCE_PREFIX}{persistenceId}");
        }

        public async Task<Guid> PersistString(DateTimeOffset expiry, string persistedString)
        {
            var newId = Guid.NewGuid();
            var newSecret = new KeyVaultSecret($"{PERSISTENCE_PREFIX}{newId}", persistedString);
            newSecret.Properties.ExpiresOn = expiry;

            await GetClient().SetSecretAsync(newSecret);
            return newId;
        }

        public async Task<string> RetrieveString(Guid persistenceId)
        {
            var secret = await GetClient().GetSecretAsync($"{PERSISTENCE_PREFIX}{persistenceId}");
            if (secret == null || secret.Value == null)
                throw new Exception("Secret not found");
            return secret.Value.Value;
        }

        private SecretClient GetClient()
        {
            return new SecretClient(new Uri($"https://{_vaultName}.vault.azure.net/"),
                _credentialProvider.Get(MultiCredentialProvider.CredentialType.AgentServicePrincipal)
                                   .DefaultAzureCredential);
        }
    }
}
