using Azure.Identity;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    public class AccessTokenRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var credential = new DefaultAzureCredential(false);

            var target = newEntity.GetTarget<Targets.AccessTokenTarget>();
            var accessToken = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new string[] { target.Resource }));
            newEntity.LastChanged = DateTime.Now;
            newEntity.KeyValidPeriod = accessToken.ExpiresOn - newEntity.LastChanged;

            newEntity.UpdatedKey = accessToken.Token;

            return newEntity;
        }
    }
}
