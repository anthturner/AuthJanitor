using System;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.SecureStorageProviders
{
    public interface ISecureStorageProvider
    {
        Task<Guid> Persist<T>(DateTimeOffset expiry, T persistedObject);
        Task<T> Retrieve<T>(Guid persistenceId);
        Task Destroy(Guid persistenceId);
    }
}
