using AuthorizationJanitor.Shared.Configuration;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.RotationStrategies
{
    /// <summary>
    /// Requests an Access Token for a given set of scopes. The new token is committed to the AppSecrets Key Vault.
    /// </summary>
    public class AccessTokenRotationStrategy : RotationStrategy<AccessTokenRotationStrategy.Configuration>
    {
        /// <summary>
        /// Configuration options for AccessTokenRotationStrategy
        /// </summary>
        public class Configuration : IRotationConfiguration
        {
            /// <summary>
            /// Scopes to be requested for Access Token
            /// </summary>
            public string[] TokenScopes { get; set; }
        }

        /// <summary>
        /// Requests an Access Token for a given set of scopes. The new token is committed to the AppSecrets Key Vault.
        /// </summary>
        /// <param name="logger">Event logger</param>
        /// <param name="configuration">AppSecret Configuration</param>
        public AccessTokenRotationStrategy(ILogger logger, AppSecretConfiguration configuration) : base(logger, configuration) { }

        /// <summary>
        /// Acquire a token for the first time
        /// </summary>
        /// <param name="forceRegeneration">This parameter is ignored; a token is generated no matter what</param>
        /// <returns>New Access Token to commit to Key Vault</returns>
        public override async Task<string> CreateInitialData(bool forceRegeneration)
        {
            Logger.LogInformation("Acquiring access token for the first time...");
            return await Rotate();
        }

        /// <summary>
        /// Request a new Access Token with the necessary scopes
        /// </summary>
        /// <returns>New Access Token to commit to Key Vault</returns>
        public override async Task<string> Rotate()
        {
            Logger.LogInformation("Acquiring access token for the following token scopes: {0}", string.Join(", ", RotationConfiguration.TokenScopes));
            try
            {
                var token = await GetAccessToken(RotationConfiguration.TokenScopes);
                AppSecretConfiguration.LastChanged = DateTimeOffset.Now;
                AppSecretConfiguration.AppSecretValidPeriod = token.ExpiresOn - AppSecretConfiguration.LastChanged;
                Logger.LogInformation("Successfully acquired access token. Token expires at {0}", token.ExpiresOn);
                return token.Token;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Exception thrown while acquiring access token");
                return null;
            }
        }

        /// <summary>
        /// Sanity check ability to request an Access Token with the necessary scopes
        /// </summary>
        /// <returns><c>TRUE</c> if able to rotate</returns>
        public override async Task<bool> SanityCheck()
        {
            Logger.LogInformation("Testing ability to acquire access token for the following token scopes: {0}", string.Join(", ", RotationConfiguration.TokenScopes));
            try
            {
                var token = await GetAccessToken(RotationConfiguration.TokenScopes);
                Logger.LogInformation("Token successfully acquired!");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unable to acquire access token!");
                return false;
            }
        }

        private ValueTask<Azure.Core.AccessToken> GetAccessToken(params string[] scopes) =>
            new DefaultAzureCredential(false).GetTokenAsync(new Azure.Core.TokenRequestContext(scopes));
    }
}
