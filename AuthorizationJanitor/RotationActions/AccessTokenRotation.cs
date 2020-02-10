using Azure.Identity;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    /// <summary>
    /// Requests a new Access Token for a given Resource and commits the token to the AppSecrets Key Vault
    /// </summary>
    public class AccessTokenRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var credential = new DefaultAzureCredential(false);

            var target = newEntity.GetTarget<Targets.AccessTokenTarget>();
            var accessToken = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new string[] { target.Resource }));
            newEntity.LastChanged = DateTime.Now;
            newEntity.AppSecretValidPeriod = accessToken.ExpiresOn - newEntity.LastChanged;

            newEntity.UpdatedAppSecret = accessToken.Token;

            return newEntity;
        }
    }
}
