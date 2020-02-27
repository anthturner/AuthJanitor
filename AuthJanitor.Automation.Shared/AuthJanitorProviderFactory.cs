using AuthJanitor.Providers;
using System;

namespace AuthJanitor.Automation.Shared
{
    public static class AuthJanitorProviderFactory
    {
        public static T CreateFromResource<T>(Resource resource) where T : class, IAuthJanitorProvider
        {
            IAuthJanitorProvider provider = HelperMethods.GetProvider(resource.ProviderType);
            provider.SerializedConfiguration = resource.ProviderConfiguration;
            return provider as T;
        }

        public static AuthJanitorProviderConfiguration CreateProviderConfiguration(string type)
        {
            return CreateProviderConfiguration(GetProviderType(type));
        }

        public static AuthJanitorProviderConfiguration CreateProviderConfiguration(Type type)
        {
            return Activator.CreateInstance(GetConfigurationType(type)) as AuthJanitorProviderConfiguration;
        }

        private static Type GetProviderType(string type)
        {
            return HelperMethods.GetProvider(type)?.GetType();
        }

        private static Type GetConfigurationType(Type extensionType)
        {
            if (extensionType == null ||
                !typeof(IAuthJanitorProvider).IsAssignableFrom(extensionType) ||
                !extensionType.BaseType.IsGenericType ||
                !typeof(AuthJanitorProviderConfiguration).IsAssignableFrom(extensionType.BaseType.GetGenericArguments()[0]))
            {
                throw new Exception("Extension or Extension Configuration Type invalid!");
            }
            else
            {
                return extensionType.BaseType.GetGenericArguments()[0];
            }
        }
    }
}
