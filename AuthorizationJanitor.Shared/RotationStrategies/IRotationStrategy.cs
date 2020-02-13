using AuthorizationJanitor.Shared.Configuration;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    public interface IRotationConfiguration
    {
    }

    public interface IRotationStrategy
    {
        /// <summary>
        /// AppSecret Configuration
        /// </summary>
        AppSecretConfiguration AppSecretConfiguration { get; }

        /// <summary>
        /// Create the initial key or secret for a given service, if needed
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, any existing key 
        /// or secret will be rotated as part of this creation. Otherwise, an 
        /// existing entity will be left alone until the next expiry time.</param>
        /// <returns>New AppSecret value to commit to Key Vault</returns>
        Task<string> CreateInitialData(bool forceRegeneration);

        /// <summary>
        /// Rotate key or secret
        /// </summary>
        /// <returns>New AppSecret value to commit to Key Vault</returns>
        Task<string> Rotate();
        
        /// <summary>
        /// Sanity check RotationStrategy to ensure AuthJanitor can execute a rotation
        /// </summary>
        /// <returns><c>TRUE</c> if AuthJanitor is able to rotate</returns>
        Task<bool> SanityCheck();
    }
}