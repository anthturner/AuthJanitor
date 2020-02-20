using AuthorizationJanitor.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public class ManagedSecretStore
    {
        public Task<List<ManagedSecret>> GetManagedSecrets()
        {
            return Task.FromResult(new List<ManagedSecret>());
        }
        public Task<ManagedSecret> GetManagedSecret(Guid id)
        {
            return Task.FromResult(new ManagedSecret());
        }

        public async Task<IConsumingApplicationExtension> CreateExtensionFromManagedSecret(ILogger logger, Guid id)
        {
            var secret = await GetManagedSecret(id);

            if (secret.ConsumingApplicationType.IsAbstract || !secret.ConsumingApplicationType.IsSubclassOf(typeof(IConsumingApplicationExtension)))
            {
                throw new Exception($"ConsumingApplication type '{secret.ConsumingApplicationType.Name}' is not valid because it is abstract or does not implement IConsumingApplicationExtension");
            }

            if (secret.RekeyableServiceType.IsAbstract || !secret.RekeyableServiceType.IsSubclassOf(typeof(IRekeyableServiceExtension)))
            {
                throw new Exception($"RekeyableService type '{secret.RekeyableServiceType.Name}' is not valid because it is abstract or does not implement IRekeyableServiceExtension");
            }

            object rekeyableConfiguration = JsonConvert.DeserializeObject(secret.RekeyableServiceConfiguration, secret.RekeyableServiceType.GetGenericArguments()[0]);
            object consumerConfiguration = JsonConvert.DeserializeObject(secret.ConsumingApplicationConfiguration, secret.ConsumingApplicationType.GetGenericArguments()[0]);

            object rekeyable = Activator.CreateInstance(secret.RekeyableServiceType, new object[] { logger, rekeyableConfiguration });
            object consumer = Activator.CreateInstance(secret.ConsumingApplicationType, new object[] { logger, rekeyable, consumerConfiguration });
            return consumer as IConsumingApplicationExtension;
        }
    }
}
