using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AuthJanitor.Automation.Shared.ViewModels
{
    public class ManagedSecretViewModel : IAuthJanitorViewModel
    {
        public Guid ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        [JsonIgnore]
        public TaskConfirmationStrategies TaskConfirmationStrategies
        {
            get => (TaskConfirmationStrategies)TaskConfirmationStrategiesInt;
            set => TaskConfirmationStrategiesInt = (int)value;
        }

        public int TaskConfirmationStrategiesInt { get; set; }

        public DateTimeOffset? LastChanged { get; set; }
        public int ValidPeriodMinutes { get; set; }

        [JsonIgnore]
        public TimeSpan ValidPeriod
        {
            get => TimeSpan.FromMinutes(ValidPeriodMinutes);
            set => ValidPeriodMinutes = (int)Math.Ceiling(value.TotalMinutes);
        }

        [JsonIgnore]
        public DateTime Expiry => (LastChanged.GetValueOrDefault() + ValidPeriod).DateTime;

        [JsonIgnore]
        public string ProviderSummary => $"{Resources.Count(r => !r.IsRekeyableObjectProvider)} ALCs, " +
                                         $"{Resources.Count(r => r.IsRekeyableObjectProvider)} RKOs";

        [JsonIgnore]
        public int ExpiryPercent => Expiry > DateTime.Now ? 100 : (int)(((double)(DateTime.Now - LastChanged.GetValueOrDefault()).TotalSeconds) / ValidPeriod.TotalSeconds) * 100;

        public string Nonce { get; set; }

        public string ResourceIds { get; set; } = string.Empty;
        public IEnumerable<ResourceViewModel> Resources { get; set; } = new List<ResourceViewModel>();

        public ManagedSecretViewModel()
        {
            ObjectId = Guid.Empty;
            Name = "New Managed Secret";
            Description = "Manages a secret between a rekeyable resource and application.";
            TaskConfirmationStrategies = TaskConfirmationStrategies.None;
            ValidPeriodMinutes = 60 * 24 * 90; // 90 days
            Nonce = string.Empty;
        }
    }
}
