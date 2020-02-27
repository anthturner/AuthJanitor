using AuthJanitor.Automation.Shared;
using AuthJanitor.Providers;
using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.AdminApi.Resources
{
    public class ResourceSummary
    {
        public Guid ObjectId { get; set; }
        public string ResourceName { get; set; }
        public string Description { get; set; }
        public string ProviderName { get; set; }

        public string ActionDescription { get; set; }
        public IList<RiskyConfigurationItem> Risks { get; set; } = new List<RiskyConfigurationItem>();
        public bool TestResult { get; set; }

        public ResourceSummary(Resource resource)
        {
            ObjectId = resource.ObjectId;
            ResourceName = resource.Name;
            Description = resource.Description;
            ProviderName = resource.ProviderType;
        }
    }
}
