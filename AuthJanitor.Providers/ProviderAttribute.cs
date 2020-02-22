using System;

namespace AuthJanitor.Providers
{
    public class ProviderAttribute : Attribute
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }
        public string Description { get; set; }
    }
}
