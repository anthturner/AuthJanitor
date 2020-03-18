using AuthJanitor.Automation.Shared.ViewModels;
using AuthJanitor.Providers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AuthJanitor.Automation.Shared
{
    public static class ViewModelFactory
    {
        private static IDictionary<Type, ProviderConfigurationItemViewModel.InputTypes> InputTypes { get; } = new Dictionary<Type, ProviderConfigurationItemViewModel.InputTypes>()
        {
            { typeof(string), ProviderConfigurationItemViewModel.InputTypes.Text },
            { typeof(string[]), ProviderConfigurationItemViewModel.InputTypes.TextArray },
            { typeof(int), ProviderConfigurationItemViewModel.InputTypes.Integer },
            { typeof(bool), ProviderConfigurationItemViewModel.InputTypes.Boolean },
            { typeof(Enum), ProviderConfigurationItemViewModel.InputTypes.Enumeration }
        };
        private static IDictionary<Type, Func<object, PropertyInfo, string>> ValueReaders { get; } = new Dictionary<Type, Func<object, PropertyInfo, string>>()
        {
            { typeof(string), (instance, property) => property.GetValue(instance) as string },
            { typeof(string[]), (instance, property) => property.GetValue(instance) == null ? string.Empty : string.Join(",", property.GetValue(instance) as string[]) },
            { typeof(int), (instance, property) => (property.GetValue(instance) as int?).GetValueOrDefault(0).ToString() },
            { typeof(bool), (instance, property) => (property.GetValue(instance) as bool?).GetValueOrDefault(false).ToString() },
            { typeof(Enum), (instance, property) => (property.GetValue(instance) as Enum).ToString() }
        };

        public static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<Func<LoadedProviderMetadata, LoadedProviderViewModel>>(serviceProvider => provider => GetViewModel(serviceProvider, provider));
            serviceCollection.AddTransient<Func<ManagedSecret, ManagedSecretViewModel>>(serviceProvider => secret => GetViewModel(serviceProvider, secret));
            serviceCollection.AddTransient<Func<Resource, ResourceViewModel>>(serviceProvider => resource => GetViewModel(serviceProvider, resource));
            serviceCollection.AddTransient<Func<RekeyingTask, RekeyingTaskViewModel>>(serviceProvider => rekeyingTask => GetViewModel(serviceProvider, rekeyingTask));
            serviceCollection.AddTransient<Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>>>(serviceProvider => config => GetViewModel(serviceProvider, config));
        }

        private static IEnumerable<ProviderConfigurationItemViewModel> GetViewModel(IServiceProvider serviceProvider, AuthJanitorProviderConfiguration config) =>
            config.GetType().GetProperties()
                    .Select(property =>
                    {
                        if (!InputTypes.Any(t => t.Key.IsAssignableFrom(property.PropertyType)) ||
                            !ValueReaders.Any(v => v.Key.IsAssignableFrom(property.PropertyType)))
                            throw new NotImplementedException($"Provider Configuration includes Type '{property.PropertyType.Name}', which is not supported");

                        var inputType = InputTypes.First(t => t.Key.IsAssignableFrom(property.PropertyType)).Value;
                        var valueReader = ValueReaders.First(t => t.Key.IsAssignableFrom(property.PropertyType)).Value;

                        return new ProviderConfigurationItemViewModel()
                        {
                            Name = property.Name,
                            DisplayName = property.GetCustomAttribute<DisplayNameAttribute>() == null ?
                                          property.Name :
                                          property.GetCustomAttribute<DisplayNameAttribute>().DisplayName,
                            HelpText = property.GetCustomAttribute<DescriptionAttribute>() == null ?
                                          string.Empty :
                                          property.GetCustomAttribute<DescriptionAttribute>().Description,
                            InputType = inputType,
                            Options = inputType == ProviderConfigurationItemViewModel.InputTypes.Enumeration ?
                                      property.PropertyType.GetEnumValues().Cast<Enum>()
                                              .ToDictionary(
                                                    k => k.ToString(),
                                                    v => HelperMethods.GetEnumValueAttribute<DisplayNameAttribute>(v) == null ?
                                                         v.ToString() :
                                                         HelperMethods.GetEnumValueAttribute<DisplayNameAttribute>(v).DisplayName)
                                              .Select(i => new ProviderConfigurationItemViewModel.SelectOption(i.Key, i.Value)) :
                                      new List<ProviderConfigurationItemViewModel.SelectOption>(),
                            Value = valueReader(config, property)
                        };
                    });

        private static LoadedProviderViewModel GetViewModel(IServiceProvider serviceProvider, LoadedProviderMetadata provider) =>
                new LoadedProviderViewModel()
                {
                    AssemblyFullName = provider.AssemblyFullName,
                    Details = provider.Details,
                    IsRekeyableObjectProvider = provider.IsRekeyableObjectProvider,
                    OriginatingFile = Path.GetFileName(provider.OriginatingFile),
                    ProviderTypeName = provider.ProviderTypeName
                };

        private static ManagedSecretViewModel GetViewModel(IServiceProvider serviceProvider, ManagedSecret secret)
        {
            var resources = secret.ResourceIds
                                .Select(resourceId => serviceProvider.GetRequiredService<IDataStore<Resource>>()
                                                                    .Get(resourceId))
                                .Select(resource => serviceProvider.GetRequiredService<Func<Resource, ResourceViewModel>>()(resource));
            foreach (var resource in resources)
            {
                var provider = serviceProvider.GetRequiredService<Func<string, IAuthJanitorProvider>>()(resource.ProviderType);
                provider.SerializedConfiguration = resource.SerializedProviderConfiguration;
                resource.Risks = provider.GetRisks(secret.ValidPeriod);
                resource.Description = provider.GetDescription();
            }
            return new ManagedSecretViewModel()
            {
                ObjectId = secret.ObjectId,
                Name = secret.Name,
                Description = secret.Description,
                TaskConfirmationStrategies = secret.TaskConfirmationStrategies,
                LastChanged = secret.LastChanged,
                ValidPeriod = secret.ValidPeriod,
                Nonce = secret.Nonce,
                Resources = resources
            };
        }

        private static RekeyingTaskViewModel GetViewModel(IServiceProvider serviceProvider, RekeyingTask rekeyingTask) =>
            new RekeyingTaskViewModel()
            {
                ObjectId = rekeyingTask.ObjectId,
                Queued = rekeyingTask.Queued,
                Expiry = rekeyingTask.Expiry,
                ManagedSecrets = rekeyingTask.ManagedSecretIds
                                      .Select(secretId => serviceProvider.GetRequiredService<IDataStore<ManagedSecret>>()
                                                                         .Get(secretId))
                                      .Select(secret => serviceProvider.GetRequiredService<Func<ManagedSecret, ManagedSecretViewModel>>()(secret))
            };

        private static ViewModels.ResourceViewModel GetViewModel(IServiceProvider serviceProvider, Resource resource)
        {
            var provider = serviceProvider.GetRequiredService<Func<string, IAuthJanitorProvider>>()(resource.ProviderType);
            provider.SerializedConfiguration = resource.ProviderConfiguration;

            return new ViewModels.ResourceViewModel()
            {
                ObjectId = resource.ObjectId,
                Name = resource.Name,
                Description = resource.Description,
                IsRekeyableObjectProvider = resource.IsRekeyableObjectProvider,
                ProviderType = resource.ProviderType,
                ProviderDetail = serviceProvider.GetRequiredService<Func<string, LoadedProviderMetadata>>()(resource.ProviderType).Details,
                ProviderConfiguration = serviceProvider.GetRequiredService<Func<AuthJanitorProviderConfiguration, IEnumerable<ProviderConfigurationItemViewModel>>>()
                    (
                        ((AuthJanitorProviderConfiguration)JsonConvert.DeserializeObject(
                            resource.ProviderConfiguration,
                            serviceProvider.GetRequiredService<Func<string, AuthJanitorProviderConfiguration>>()(resource.ProviderType).GetType()))
                    ),
                SerializedProviderConfiguration = resource.ProviderConfiguration,
                RuntimeDescription = provider.GetDescription(),
                Risks = provider.GetRisks()
            };
        }
    }
}
