using AuthJanitor.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.Shared.ViewModels
{
    public class ResourceViewModel
    {
        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsRekeyableObjectProvider { get; set; }
        public string ProviderType { get; set; }
        public ProviderAttribute ProviderDetail { get; set; }
        public IEnumerable<ProviderConfigurationItemViewModel> ProviderConfiguration { get; set; } = new List<ProviderConfigurationItemViewModel>();
        public IEnumerable<RiskyConfigurationItem> Risks { get; set; } = new List<RiskyConfigurationItem>();
        public string RuntimeDescription { get; set; }
        public int RiskScore => Risks.Sum(r => r.Score);
    }
}
