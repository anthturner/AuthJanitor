using AuthJanitor.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Logging;

[assembly: WebJobsStartup(typeof(AuthJanitor.Automation.AdminApi.Startup))]
namespace AuthJanitor.Automation.AdminApi
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            HelperMethods.InitializeServiceProvider(new LoggerFactory());
        }
    }
}
