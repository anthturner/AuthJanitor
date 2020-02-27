using AuthJanitor.Automation.AdminApi.Providers;
using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public class ResourceViewModel
    {
        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsRekeyableObjectProvider { get; set; }
        public string ProviderType { get; set; }
        public ProviderAttribute ProviderDetail { get; set; }
        public IList<ProviderConfigurationItemViewModel> ProviderConfiguration { get; set; } = new List<ProviderConfigurationItemViewModel>();
        public IList<RiskyConfigurationItem> Risks { get; set; } = new List<RiskyConfigurationItem>();
        public string RuntimeDescription { get; set; }
        public int RiskScore => Risks.Sum(r => r.Score);

        public static ResourceViewModel FromResourceWithConfiguration(Resource resource, TimeSpan riskTimespan = default(TimeSpan))
        {
            var model = FromResource(resource);
            var config = 
                JsonConvert.DeserializeObject(
                    resource.ProviderConfiguration,
                    AuthJanitorProviderFactory.CreateProviderConfiguration(resource.ProviderType).GetType())
                as AuthJanitorProviderConfiguration;

            foreach (var property in config.GetType().GetProperties())
                model.ProviderConfiguration.Add(ProviderConfigurationItemViewModel.FromProperty(property, config));

            var instance = HelperMethods.GetProvider(model.ProviderType);
            instance.SerializedConfiguration = resource.ProviderConfiguration;
            model.RuntimeDescription = instance.GetDescription();
            if (riskTimespan != default(TimeSpan))
                model.Risks = instance.GetRisks(riskTimespan);

            return model;
        }

        public static ResourceViewModel FromResource(Resource resource)
        {
             return new ResourceViewModel()
            {
                ObjectId = resource.ObjectId,
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                ProviderType = resource.ProviderType,
                ProviderDetail = 
                    HelperMethods.GetProvider(resource.ProviderType) == null ? null :
                    HelperMethods.GetProvider(resource.ProviderType).ProviderMetadata
            };
        }
    }
}
