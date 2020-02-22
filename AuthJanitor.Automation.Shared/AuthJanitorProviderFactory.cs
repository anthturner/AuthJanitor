using AuthJanitor.Providers;
using System;

namespace AuthJanitor.Automation.Shared
{
    public static class AuthJanitorProviderFactory
    {
        public static T CreateFromResource<T>(Resource resource) where T : class, IAuthJanitorProvider
        {
            IAuthJanitorProvider provider = HelperMethods.GetProvider(resource.ProviderName);
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
            return HelperMethods.GetProvider(type).GetType();
        }

        private static Type GetConfigurationType(Type extensionType)
        {
            if (extensionType == null ||
                !extensionType.IsSubclassOf(typeof(IAuthJanitorProvider)) ||
                !extensionType.IsGenericType ||
                !extensionType.GetGenericArguments()[0].IsSubclassOf(typeof(AuthJanitorProviderConfiguration)))
            {
                throw new Exception("Extension or Extension Configuration Type invalid!");
            }
            else
            {
                return extensionType.GetGenericArguments()[0];
            }
        }
    }
}
