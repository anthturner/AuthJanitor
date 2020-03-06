using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Blazor.Hosting;
using System;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Blazor
{
    public class Program
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddBlazorise(options => options.ChangeTextOnKeyPress = true)
                            .AddBootstrapProviders()
                            .AddFontAwesomeIcons();

            builder.RootComponents.Add<App>("app");

            var host = builder.Build();

            host.Services
              .UseBootstrapProviders()
              .UseFontAwesomeIcons();

            await host.RunAsync();
        }
    }
}
