using AuthJanitor.Providers;

namespace AuthJanitor.Automation.Shared.ViewModels
{
    public class LoadedProviderViewModel
    {
        public string OriginatingFile { get; set; }
        public string AssemblyFullName { get; set; }
        public string ProviderTypeName { get; set; }
        public bool IsRekeyableObjectProvider { get; set; }
        public ProviderAttribute Details { get; set; }
    }
}
