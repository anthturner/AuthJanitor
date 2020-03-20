using AuthJanitor.Automation.Shared.ViewModels;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public class DashboardMetrics : IAuthJanitorViewModel
    {
        public int TotalSecrets { get; set; }
        public int TotalResources { get; set; }
        public int TotalExpiringSoon { get; set; }
        public int TotalExpired { get; set; }

        public int TotalPendingApproval { get; set; }
        public int PercentExpired { get; set; }

        public int Risk0 { get; set; }
        public int Risk35 { get; set; }
        public int Risk60 { get; set; }
        public int Risk85 { get; set; }
        public int RiskOver85 { get; set; }

        public IEnumerable<ManagedSecretViewModel> ExpiringSoon { get; set; } = new List<ManagedSecretViewModel>();
    }
}
