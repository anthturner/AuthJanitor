using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AuthJanitor.Providers
{
    public static class ProviderFactory
    {
        public static void ConfigureProviderServices(IServiceCollection serviceCollection, IEnumerable<Type> loadedProviderTypes)
        {
            var loadedProviders = loadedProviderTypes
                    .Where(type => !type.IsAbstract && typeof(IAuthJanitorProvider).IsAssignableFrom(type))
                    .Select(type => new LoadedProviderMetadata()
                    {
                        OriginatingFile = Path.GetFileName(type.Assembly.Location),
                        AssemblyFullName = type.Assembly.FullName,
                        ProviderTypeName = type.AssemblyQualifiedName,
                        ProviderType = type,
                        ProviderConfigurationType = type.BaseType.GetGenericArguments()[0],
                        Details = type.GetCustomAttribute<ProviderAttribute>()
                    });

            serviceCollection.AddTransient<Func<string, IAuthJanitorProvider>>(serviceProvider => key =>
            {
                var selectedProvider = loadedProviders.FirstOrDefault(p => p.ProviderTypeName == key);
                if (selectedProvider == null) throw new Exception($"Provider '{key}' not available!");
                return Activator.CreateInstance(selectedProvider.ProviderType, new object[] {
                    serviceProvider.GetRequiredService<ILoggerFactory>(),
                    serviceProvider
                }) as IAuthJanitorProvider;
            });

            serviceCollection.AddTransient<Func<string, AuthJanitorProviderConfiguration>>(serviceProvider => key =>
            {
                var selectedProvider = loadedProviders.FirstOrDefault(p => p.ProviderTypeName == key);
                if (selectedProvider == null) throw new Exception($"Provider '{key}' not available!");
                return Activator.CreateInstance(selectedProvider.ProviderConfigurationType) as AuthJanitorProviderConfiguration;
            });

            serviceCollection.AddTransient<Func<string, LoadedProviderMetadata>>(serviceProvider => key =>
                loadedProviders.FirstOrDefault(p => p.ProviderTypeName == key));
            serviceCollection.AddSingleton<List<LoadedProviderMetadata>>(loadedProviders.ToList()); ;
        }
    }
}
