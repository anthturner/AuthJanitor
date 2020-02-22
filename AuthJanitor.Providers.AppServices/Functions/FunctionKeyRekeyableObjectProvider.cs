using System;
using System.Threading.Tasks;

namespace AuthJanitor.Providers.AppServices.Functions
{
    public class FunctionKeyRekeyableObjectProvider : RekeyableObjectProvider<FunctionKeyConfiguration>
    {
        public override async Task<RegeneratedSecret> Rekey(TimeSpan requestedValidPeriod)
        {
            RegeneratedSecret newKey = new RegeneratedSecret()
            {
                Expiry = DateTimeOffset.Now + requestedValidPeriod,
                UserHint = Configuration.UserHint,
                NewSecretValue = HelperMethods.GenerateCryptographicallySecureString(Configuration.KeyLength)
            };

            // Apply the new Key
            Microsoft.Azure.Management.AppService.Fluent.IFunctionApp functionsApp = await (await GetAzure()).AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, ResourceName);
            await functionsApp.AddFunctionKeyAsync(Configuration.FunctionName, Configuration.FunctionKeyName, newKey.NewSecretValue);

            return newKey;
        }

        public override string GetDescription()
        {
            return $"Rekey Azure Function '{Configuration.FunctionName}' key name '{Configuration.FunctionKeyName}' with new {Configuration.KeyLength}-length key." + Environment.NewLine + base.GetDescription();
        }
    }
}
