using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Extensions.AppServices.Functions
{
    public class FunctionRekeyableService : RekeyableServiceExtension<FunctionKeyConfiguration>
    {
        public FunctionRekeyableService(ILogger logger, FunctionKeyConfiguration configuration) : base(logger, configuration) { }

        public override async Task<RegeneratedKey> Rekey()
        {
            RegeneratedKey newKey = new RegeneratedKey()
            {
                NewKey = HelperMethods.GenerateCryptographicallySecureString(Configuration.KeyLength)
            };

            // Apply the new Key
            Microsoft.Azure.Management.AppService.Fluent.IFunctionApp functionsApp = await (await GetAzure()).AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);
            await functionsApp.AddFunctionKeyAsync(Configuration.FunctionName, Configuration.FunctionKeyName, newKey.NewKey);

            return newKey;
        }

        public override string GetDescription()
        {
            return $"Rekey Azure Function '{Configuration.FunctionName}' key name '{Configuration.FunctionKeyName}' with new {Configuration.KeyLength}-length key." + Environment.NewLine + base.GetDescription();
        }
    }
}
