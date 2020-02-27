using AuthJanitor.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace AuthJanitor.Automation.AdminApi.Providers
{
    public class ProviderConfigurationItemViewModel
    {
        public enum InputTypes
        {
            Text,
            Boolean,
            Select
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public InputTypes InputType { get; set; }
        public string HelpText { get; set; }
        public string Value { get; set; }
        public List<object> Options { get; set; } = new List<object>();

        public static ProviderConfigurationItemViewModel FromProperty(PropertyInfo property, object instance = null)
        {
            var description = property.GetCustomAttribute<DescriptionAttribute>();
            var displayName = property.GetCustomAttribute<DisplayNameAttribute>();

            Dictionary<string, string> options = new Dictionary<string, string>();
            ProviderConfigurationItemViewModel.InputTypes inputType;
            if (property.PropertyType == typeof(string))
                inputType = ProviderConfigurationItemViewModel.InputTypes.Text;
            else if (property.PropertyType == typeof(bool))
                inputType = ProviderConfigurationItemViewModel.InputTypes.Boolean;
            else if (typeof(Enum).IsAssignableFrom(property.PropertyType))
            {
                inputType = ProviderConfigurationItemViewModel.InputTypes.Select;
                foreach (var value in property.PropertyType.GetEnumValues())
                {
                    var optionDisplayName = HelperMethods.GetEnumValueAttribute<DisplayNameAttribute>(value as Enum);
                    if (optionDisplayName == null)
                        options.Add(value.ToString(), value.ToString());
                    else
                        options.Add(
                            value.ToString(),
                            string.IsNullOrEmpty(optionDisplayName.DisplayName) ? value.ToString() : optionDisplayName.DisplayName);
                }
            }
            else return null;

            return new ProviderConfigurationItemViewModel()
            {
                Name = property.Name,
                DisplayName = displayName != null ? displayName.DisplayName : property.Name,
                HelpText = description != null ? description.Description : string.Empty,
                InputType = inputType,
                Options = options.Select(o => new { key = o.Key, value = o.Value } as object).ToList(),
                Value = (instance == null || property.GetValue(instance) == null) ? 
                        string.Empty : property.GetValue(instance).ToString()
            };
        }
    }
}
