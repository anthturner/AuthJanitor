using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.PersistenceEncryption
{
    public interface IPersistenceEncryption
    {
        Task<string> Encrypt(string salt, string plainText);
        Task<string> Decrypt(string salt, string cipherText);
    }
}
