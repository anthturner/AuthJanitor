using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared
{
    public static class HelperMethods
    {
        private const string CHARS_ALPHANUMERIC_ONLY = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static string GenerateCryptographicallySecureString(int length, string chars = CHARS_ALPHANUMERIC_ONLY)
        {
            // Cryptography Tip!
            // https://cmvandrevala.wordpress.com/2016/09/24/modulo-bias-when-generating-random-numbers/
            // Using modulus to wrap around the source string tends to mathematically favor lower index values for
            //   smaller values of RAND_MAX (here it is LEN(chars)=62). To overcome this bias, we generate the randomness as
            //   4 bytes (int32) per single character we need, to maximize the value of RAND_MAX inside the RNG (as Int32.Max).
            //   Once the value comes out, though, we can introduce modulus again because RAND_MAX is based on the
            //   entropy going into the byte array rather than a fixed set (0,LEN(chars)) -- that makes it sufficiently
            //   large to overcome bias as seen by chi-squared. (Bias approaching zero)
            // * There is some evidence to suggest this has been taken into account in newer versions of NET. *

            byte[] data = new byte[4 * length];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
                crypto.GetBytes(data);

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var randomNumber = BitConverter.ToInt32(data, i * 4);
                sb.Append(chars[randomNumber % chars.Length]);
            }
            return sb.ToString();
        }

        public static string SHA256HashString(string str)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static Task<IAzure> GetAzure()
        {
            return Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(SdkContext.AzureCredentialsFactory.FromMSI(new MSILoginInformation(MSIResourceType.AppService), AzureEnvironment.AzureGlobalCloud))
                .WithDefaultSubscriptionAsync();
        }

        public static string GetEnumString(this Enum enumValue) =>
            enumValue.GetType().GetField(enumValue.ToString()).GetCustomAttribute<DescriptionAttribute>()?.Description;

        //public static async Task<Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult> GetToken(HttpRequest req, string resource, string clientId, string clientSecret)
        //{
        //    string token = await req.HttpContext.GetTokenAsync("access_token");
        //    string assertionType = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        //    string userName =
        //        req.HttpContext.User.FindFirst(ClaimTypes.Upn)?.Value ??
        //        req.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        //    var userAssertion = new Microsoft.IdentityModel.Clients.ActiveDirectory.UserAssertion(token, assertionType, userName);

        //    var authContext = new AuthenticationContext($"https://login.microsoftonline.com/common");
        //    var clientCredential = new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(clientId, clientSecret);
        //    var result = await authContext.AcquireTokenAsync(resource, clientCredential, userAssertion);
        //    return result;
        //}
    }
}
