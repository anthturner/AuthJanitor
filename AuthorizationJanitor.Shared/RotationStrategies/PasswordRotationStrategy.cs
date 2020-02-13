using AuthorizationJanitor.Shared.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Regenerates a password based on a provided configuration. The new password is committed to the AppSecrets Key Vault.
    /// </summary>
    public class PasswordRotationStrategy : RotationStrategy<PasswordRotationStrategy.Configuration>
    {
        /// <summary>
        /// Configuration options for PasswordRotationStrategy
        /// </summary>
        public class Configuration : IRotationConfiguration
        {
            /// <summary>
            /// Password length
            /// </summary>
            public int Length { get; set; }
        }

        /// <summary>
        /// Regenerates a password based on a provided configuration. The new password is committed to the AppSecrets Key Vault.
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret Configuration</param>
        public PasswordRotationStrategy(ILogger logger, AppSecretConfiguration configuration) : base(logger, configuration) { }

        /// <summary>
        /// Cache the current password (regenerate if forced).
        /// </summary>
        /// <param name="forceRegeneration">If <c>TRUE</c>, regenerate the password immediately</param>
        /// <returns>New password or <c>NULL</c> for no change</returns>
        public override async Task<string> CreateInitialData(bool forceRegeneration)
        {
            if (forceRegeneration)
                return await Rotate();
            return null;
        }

        /// <summary>
        /// Regenerate the password from the parameters in the RotationConfiguration
        /// </summary>
        /// <returns>New password to commit to Key Vault</returns>
        public override Task<string> Rotate()
        {
            Logger.LogInformation("Creating new password of length {0}", RotationConfiguration.Length);

            AppSecretConfiguration.LastChanged = DateTime.Now;
            return Task.FromResult(
                HelperMethods.GenerateCryptographicallySecureString(RotationConfiguration.Length));
        }

        /// <summary>
        /// Sanity check ability to generate passwords. This will always be <c>TRUE</c> for this strategy.
        /// </summary>
        /// <returns>TRUE</returns>
        public override Task<bool> SanityCheck() => Task.FromResult(true);
    }
}
