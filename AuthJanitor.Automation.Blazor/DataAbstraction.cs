using AuthJanitor.Automation.Shared.ViewModels;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Blazor
{
    public static class DataAbstraction
    {
        private const string API_BASE = "/api";
        private static Dictionary<Type, string> ApiFormatStrings = new Dictionary<Type, string>()
        {
            { typeof(ManagedSecretViewModel), "/secrets" },
            { typeof(RekeyingTaskViewModel), "/tasks" },
            { typeof(ResourceViewModel), "/resources" },
            { typeof(LoadedProviderViewModel), "/providers" },
            { typeof(IEnumerable<ProviderConfigurationItemViewModel>), "/providers" }
        };

        public static HttpClient GetAJConfiguredClient(this HttpClient http)
        {
            if (!http.DefaultRequestHeaders.Contains("AuthJanitor"))
                http.DefaultRequestHeaders.Add("AuthJanitor", "administrator");
            return http;
        }

        public static async Task<T> Get<T>(this HttpClient http, Guid objectId) =>
            await ConfigureApiHttpRequest<T>(http)
                 .GetJsonAsync<T>($"{API_BASE}{ApiFormatStrings[typeof(T)]}/{objectId}");

        public static async Task<IEnumerable<T>> List<T>(this HttpClient http) =>
            await ConfigureApiHttpRequest<T>(http)
                 .GetJsonAsync<IEnumerable<T>>($"{API_BASE}{ApiFormatStrings[typeof(T)]}");

        public static async Task<T> Create<T>(this HttpClient http, T obj) =>
            await ConfigureApiHttpRequest<T>(http)
                 .PostJsonAsync<T>($"{API_BASE}{ApiFormatStrings[typeof(T)]}", obj);

        public static async Task CreateWithAlternateSerializer<T>(this HttpClient http, T obj) =>
            await ConfigureApiHttpRequest<T>(http)
                 .PostAsync($"{API_BASE}{ApiFormatStrings[typeof(T)]}", new StringContent(JsonConvert.SerializeObject(obj)));

        public static async Task<T> Update<T>(this HttpClient http, Guid objectId, T obj) =>
            await ConfigureApiHttpRequest<T>(http)
                 .PostJsonAsync<T>($"{API_BASE}{ApiFormatStrings[typeof(T)]}/{objectId}", obj);

        public static async Task Delete<T>(this HttpClient http, Guid objectId) =>
            await ConfigureApiHttpRequest<T>(http)
                 .DeleteAsync($"{API_BASE}{ApiFormatStrings[typeof(T)]}/{objectId}");

        private static HttpClient ConfigureApiHttpRequest<T>(HttpClient client)
        {
            if (!ApiFormatStrings.ContainsKey(typeof(T)))
                throw new Exception("Unsupported data abstraction!");
            return client.GetAJConfiguredClient();
        }
    }
}
