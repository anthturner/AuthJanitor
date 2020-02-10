using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    public class PasswordRotation : IRotation
    {
        public async Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity)
        {
            var newEntity = entity.Clone();
            var target = newEntity.GetTarget<Targets.KeyVaultTarget>();

            var client = new SecretClient(new Uri($"https://{target.VaultName}.vault.azure.net/"), new DefaultAzureCredential(false));
            var currentSecret = await client.GetSecretAsync(target.KeyOrSecretName);

            var newPassword = HelperMethods.GenerateCryptographicallySecureString(target.SecretLength);
            await client.SetSecretAsync(target.KeyOrSecretName, newPassword);

            newEntity.UpdatedKey = newPassword;
            newEntity.LastChanged = DateTime.Now;

            return newEntity;
        }
    }
}
