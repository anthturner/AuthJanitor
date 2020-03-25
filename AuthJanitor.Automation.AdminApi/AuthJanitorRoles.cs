using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace AuthJanitor.Automation.AdminApi
{
    public sealed class AuthJanitorRoles
    {
        public static readonly string GlobalAdmin = "globalAdmin";
        public static readonly string ResourceAdmin = "resourceAdmin";
        public static readonly string SecretAdmin = "secretAdmin";
        public static readonly string ServiceOperator = "serviceOperator";
        public static readonly string Auditor = "auditor";
    }
    public static class AuthJanitorRoleExtensions
    {
#if DEBUG
        private static bool IsRunningLocally => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
#endif

        public static bool IsValidUser(this HttpRequest req)
        {
#if DEBUG
            if (IsRunningLocally) return true;
#endif
            if (req.HttpContext.User == null ||
                req.HttpContext.User.Claims == null ||
                !req.HttpContext.User.Claims.Any()) return false;
            var roles = req.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "roles");
            if (roles == null || roles.Value == null || !roles.Value.Any()) return false;

            if (roles.Value.Contains(AuthJanitorRoles.GlobalAdmin) ||
                roles.Value.Contains(AuthJanitorRoles.ResourceAdmin) ||
                roles.Value.Contains(AuthJanitorRoles.SecretAdmin) ||
                roles.Value.Contains(AuthJanitorRoles.ServiceOperator) ||
                roles.Value.Contains(AuthJanitorRoles.Auditor))
                return true;
            return false;
        }
        public static bool IsValidUser(this HttpRequest req, params string[] validRoles)
        {
            if (req.HttpContext.User == null ||
                req.HttpContext.User.Claims == null ||
                !req.HttpContext.User.Claims.Any()) return false;
            var roles = req.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "roles");
            if (roles == null || roles.Value == null || !roles.Value.Any()) return false;

            if (validRoles.Any(validRole => roles.Value.Contains(validRole)))
                return true;
            return false;
        }
    }
}
