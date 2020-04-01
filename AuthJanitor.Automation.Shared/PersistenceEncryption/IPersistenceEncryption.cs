using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.PersistenceEncryption
{
    public interface IPersistenceEncryption
    {
        /// <summary>
        /// Encrypt sensitive data
        /// </summary>
        /// <param name="salt">Encryption salt</param>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Encrypted ciphertext</returns>
        Task<string> Encrypt(string salt, string plainText);

        /// <summary>
        /// Decrypt sensitive data
        /// </summary>
        /// <param name="salt">Encryption salt</param>
        /// <param name="cipherText">Encrypted ciphertext</param>
        /// <returns>Decrypted text</returns>
        Task<string> Decrypt(string salt, string cipherText);
    }
}
