using System;
using System.Collections.Generic;

namespace AuthorizationJanitor.Extensions.KeyVault
{
    public class AccessTokenConfiguration : AuthorizationJanitorExtensionConfiguration
    {
        /// <summary>
        /// Scopes/Resources to request for the Access Token
        /// </summary>
        public string[] Scopes { get; set; }

        /// <summary>
        /// Get a string describing the Configuration
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptiveString()
        {
            return base.GetDescriptiveString() + Environment.NewLine +
$"Access Token Scopes: {string.Join(", ", Scopes)}";
        }

        /// <summary>
        /// Get a list of configuration choices that might be risky
        /// </summary>
        /// <returns></returns>
        public override IList<RiskyConfigurationItem> GetRiskyConfigurations()
        {
            List<RiskyConfigurationItem> issues = new List<RiskyConfigurationItem>();
            if (Scopes.Length > 10)
            {
                issues.Add(new RiskyConfigurationItem()
                {
                    Score = 0.7,
                    Risk = $"There are more than 10 ({Scopes.Length}) scopes defined for a single access token",
                    Recommendation = "Reduce the number of scopes per token by segregating access between data security boundaries in the application(s)."
                });
            }

            return issues;
        }
    }
}
