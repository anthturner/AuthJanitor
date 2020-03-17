using Microsoft.AspNetCore.Http;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi
{
    public static class Extensions
    {
        private const string HEADER_NAME = "AuthJanitor";
        private const string HEADER_VALUE = "administrator";

        public static bool PassedHeaderCheck(this HttpRequest req) => 
            req.Headers.ContainsKey(HEADER_NAME) && 
            req.Headers[HEADER_NAME].First() == HEADER_VALUE;
    }
}
