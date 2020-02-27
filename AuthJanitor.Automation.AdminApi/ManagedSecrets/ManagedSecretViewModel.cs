using AuthJanitor.Automation.AdminApi.Resources;
using AuthJanitor.Automation.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi.ManagedSecrets
{
    public class ManagedSecretViewModel
    {
        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public TaskConfirmationStrategies TaskConfirmationStrategies { get; set; }

        public DateTime LastChanged { get; set; }
        public TimeSpan ValidPeriod { get; set; }
        public DateTime Expiry => LastChanged + ValidPeriod;

        public string ProviderSummary => $"{Resources.Count(r => !r.IsRekeyableObjectProvider)} ALCs, " +
                                         $"{Resources.Count(r => r.IsRekeyableObjectProvider)} RKOs";

        public int ExpiryPercent => Expiry > DateTime.Now ? 100 : (int)(((double)(DateTime.Now - LastChanged).TotalSeconds) / ValidPeriod.TotalSeconds) * 100;

        public string Nonce { get; set; }

        public List<ResourceViewModel> Resources { get; set; } = new List<ResourceViewModel>();

        public static ManagedSecretViewModel FromManagedSecret(ManagedSecret secret, List<Resource> resources = null)
        {
            return new ManagedSecretViewModel()
            {
                ObjectId = secret.ObjectId,
                Name = secret.Name,
                Description = secret.Description,
                TaskConfirmationStrategies = secret.TaskConfirmationStrategies,
                LastChanged = secret.LastChanged,
                ValidPeriod = secret.ValidPeriod,
                Nonce = secret.Nonce,
                Resources = resources != null ?
                    resources
                    .Where(r => secret.ResourceIds.Contains(r.ObjectId))
                    .Select(r => ResourceViewModel.FromResourceWithConfiguration(r, secret.ValidPeriod))
                    .ToList() : new List<ResourceViewModel>()
            };
        }
    }
}
