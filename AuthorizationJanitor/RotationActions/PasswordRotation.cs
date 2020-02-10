using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    /// <summary>
    /// Generates a cryptographically secure alphanumeric password and commits it to the AppSecrets Key Vault
    /// </summary>
    public class PasswordRotation : IRotation
    {
        public Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var target = newEntity.GetTarget<Targets.PasswordTarget>();

            newEntity.UpdatedAppSecret = HelperMethods.GenerateCryptographicallySecureString(target.Length);
            newEntity.LastChanged = DateTime.Now;

            return Task.FromResult(newEntity);
        }
    }
}
