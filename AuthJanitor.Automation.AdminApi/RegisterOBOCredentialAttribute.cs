using AuthJanitor.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.AdminApi
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class RegisterOBOCredentialAttribute : FunctionInvocationFilterAttribute
#pragma warning restore CS0618 // Type or member is obsolete
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public override Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var request = (executingContext.Arguments.First(a => a.Value is HttpRequest)).Value as HttpRequest;

            var credentialProvider = Startup.ServiceProvider.GetRequiredService<MultiCredentialProvider>();
            // TODO: Register incoming token here!

            return base.OnExecutingAsync(executingContext, cancellationToken);
        }
    }
}
