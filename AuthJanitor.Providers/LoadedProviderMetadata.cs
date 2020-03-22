using System;
using System.Reflection;

namespace AuthJanitor.Providers
{
    public class LoadedProviderMetadata
    {
        public string OriginatingFile { get; set; }
        public string ProviderTypeName { get; set; }
        public Type ProviderType { get; set; }
        public Type ProviderConfigurationType { get; set; }
        public bool IsRekeyableObjectProvider => typeof(IRekeyableObjectProvider).IsAssignableFrom(ProviderType);

        public ProviderAttribute Details { get; set; }
        public string SvgImage { get; set; }
        public AssemblyName AssemblyName { get; set; }
    }
}
