using System;

namespace AuthJanitor.Providers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProviderAttribute : Attribute
    {
        public string Name { get; set; }
        public string IconClass { get; set; }
        public string Description { get; set; }
        public string MoreInformationUrl { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProviderImageAttribute : Attribute
    {
        public string SvgImage { get; set; }

        public ProviderImageAttribute(string svgImage) =>
            SvgImage = svgImage;
    }
}
