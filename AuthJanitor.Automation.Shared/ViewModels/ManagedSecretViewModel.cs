using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.Shared.ViewModels
{
    public class ManagedSecretViewModel
    {
        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public TaskConfirmationStrategies TaskConfirmationStrategies { get; set; }

        public DateTimeOffset LastChanged { get; set; }
        public int ValidPeriodMinutes { get; set; }

        [JsonIgnore]
        public TimeSpan ValidPeriod
        {
            get => TimeSpan.FromMinutes(ValidPeriodMinutes);
            set => ValidPeriodMinutes = (int)Math.Ceiling(value.TotalMinutes);
        }
        //public DateTimeOffset Expiry => LastChanged + ValidPeriod;
        public DateTime Expiry => (LastChanged + ValidPeriod).DateTime;

        public string ProviderSummary => $"{Resources.Count(r => !r.IsRekeyableObjectProvider)} ALCs, " +
                                         $"{Resources.Count(r => r.IsRekeyableObjectProvider)} RKOs";

        public int ExpiryPercent => Expiry > DateTime.Now ? 100 : (int)(((double)(DateTime.Now - LastChanged).TotalSeconds) / ValidPeriod.TotalSeconds) * 100;

        public string Nonce { get; set; }

        public IEnumerable<Guid> ResourceIds { get; set; } = new List<Guid>();
        public IEnumerable<ResourceViewModel> Resources { get; set; } = new List<ResourceViewModel>();
    }
}
