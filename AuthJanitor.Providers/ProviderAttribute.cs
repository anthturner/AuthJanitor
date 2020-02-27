using System;

namespace AuthJanitor.Providers
{
    public class ProviderAttribute : Attribute
    {
        public string Name { get; set; }
        public string IconClass { get; set; }
        public string Description { get; set; }
    }
}
