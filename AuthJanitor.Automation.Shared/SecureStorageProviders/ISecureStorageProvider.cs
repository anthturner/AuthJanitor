using System;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.SecureStorageProviders
{
    public interface ISecureStorageProvider
    {
        Task<Guid> PersistString(DateTimeOffset expiry, string persistedString);
        Task<string> RetrieveString(Guid persistenceId);
        Task DestroyString(Guid persistenceId);
    }
}
