using AuthorizationJanitor.Shared.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Encapsulates the logic associated with rotating a key or secret for some service
    /// </summary>
    /// <typeparam name="TRotationConfiguration">Type of Rotation Configuration</typeparam>
    public abstract class RotationStrategy<TRotationConfiguration> : IRotationStrategy where TRotationConfiguration : IRotationConfiguration
    {
        /// <summary>
        /// Configuration for the AppSecret managed by AuthJanitor
        /// </summary>
        public AppSecretConfiguration AppSecretConfiguration { get; }

        /// <summary>
        /// Configuration specific to the RotationStrategy in use
        /// </summary>
        public TRotationConfiguration RotationConfiguration { get; }

        /// <summary>
        /// Event Logger
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Handles the logic of rotating a key or secret of some service
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret configuration</param>
        public RotationStrategy(ILogger logger, AppSecretConfiguration configuration)
        {
            AppSecretConfiguration = configuration;
            RotationConfiguration = configuration.GetRotationConfiguration<TRotationConfiguration>();
            Logger = logger;
        }

        /// <summary>
        /// Sanity check RotationStrategy to ensure AuthJanitor can execute a rotation
        /// </summary>
        /// <returns><c>TRUE</c> if AuthJanitor is able to rotate</returns>
        public abstract Task<bool> SanityCheck();

        /// <summary>
        /// Create the initial key or secret for a given service, if needed
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, any existing key 
        /// or secret will be rotated as part of this creation. Otherwise, an 
        /// existing entity will be left alone until the next expiry time.</param>
        /// <returns>New AppSecret value to commit to Key Vault</returns>
        public abstract Task<string> CreateInitialData(bool forceRegeneration);

        /// <summary>
        /// Rotate key or secret
        /// </summary>
        /// <returns>New AppSecret value to commit to Key Vault</returns>
        public abstract Task<string> Rotate();
    }
}
