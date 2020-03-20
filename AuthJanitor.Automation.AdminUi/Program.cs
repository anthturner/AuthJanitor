using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using System.Net.Http;
using AuthJanitor.Automation.Shared;

namespace AuthJanitor.Automation.AdminUi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services
                .AddBlazorise(options => options.ChangeTextOnKeyPress = true)
                .AddBootstrapProviders()
                .AddFontAwesomeIcons();

            // Create both HttpClient and AuthJanitorHttpClient -- the AJ client from the HttpClient
            // ... this gets us the BaseAddress in the AJ-customized HttpClient
            builder.Services.AddBaseAddressHttpClient();
            builder.Services.AddSingleton((s) => new AuthJanitorHttpClient(s.GetRequiredService<HttpClient>()));

            builder.RootComponents.Add<App>("app");

            var host = builder.Build();

            host.Services
                .UseBootstrapProviders()
                .UseFontAwesomeIcons();
            
            await host.RunAsync();
        }
    }
}
