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
        
        [System.Text.Json.Serialization.JsonIgnore]
        public TaskConfirmationStrategies TaskConfirmationStrategies
        {
            get => (TaskConfirmationStrategies)StrategyInt;
            set => StrategyInt = (int)value;
        }

        public int StrategyInt { get; set; }

        public DateTimeOffset LastChanged { get; set; }
        public int ValidPeriodMinutes { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public TimeSpan ValidPeriod
        {
            get => TimeSpan.FromMinutes(ValidPeriodMinutes);
            set => ValidPeriodMinutes = (int)Math.Ceiling(value.TotalMinutes);
        }

        public DateTime Expiry => (LastChanged + ValidPeriod).DateTime;

        public string ProviderSummary => $"{Resources.Count(r => !r.IsRekeyableObjectProvider)} ALCs, " +
                                         $"{Resources.Count(r => r.IsRekeyableObjectProvider)} RKOs";

        public int ExpiryPercent => Expiry > DateTime.Now ? 100 : (int)(((double)(DateTime.Now - LastChanged).TotalSeconds) / ValidPeriod.TotalSeconds) * 100;

        public string Nonce { get; set; }

        public IEnumerable<Guid> ResourceIds { get; set; } = new List<Guid>();
        public IEnumerable<ResourceViewModel> Resources { get; set; } = new List<ResourceViewModel>();
    }
}
