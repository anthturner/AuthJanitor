using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.SecureStorageProviders
{
    public class NoPersistenceEncryption : IPersistenceEncryption
    {
        public Task<string> Decrypt(string salt, string cipherText) => Task.FromResult(cipherText);

        public Task<string> Encrypt(string salt, string plainText) => Task.FromResult(plainText);
    }
}
