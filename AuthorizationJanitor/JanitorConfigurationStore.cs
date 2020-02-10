using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public class JanitorConfigurationStore
    {
        private CloudBlobDirectory _configurationBlobDirectory;

        public JanitorConfigurationStore(CloudBlobDirectory configurationBlobDirectory)
        {
            _configurationBlobDirectory = configurationBlobDirectory;
        }

        public async Task<JanitorConfigurationEntity> Get(string keyName)
        {
            var blobName = HelperMethods.SHA256HashString(keyName);
            var blob = _configurationBlobDirectory.GetBlockBlobReference(blobName);
            await blob.FetchAttributesAsync();

            var bytes = new byte[blob.Properties.Length];
            await blob.DownloadToByteArrayAsync(bytes, 0);
            return JsonConvert.DeserializeObject<JanitorConfigurationEntity>(Encoding.UTF8.GetString(bytes));
        }

        public async Task Update(JanitorConfigurationEntity entity)
        {
            var blobName = HelperMethods.SHA256HashString(entity.FriendlyKeyName);
            var blob = _configurationBlobDirectory.GetBlockBlobReference(blobName);

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity));
            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
        }

        public async Task PerformLockedTask(string keyName, Func<Task> action)
        {
            var blob = GetBlob(keyName);
            var leaseId = await blob.AcquireLeaseAsync(null);
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                await blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(leaseId));
            }
        }

        public async Task<bool> IsLocked(string keyName)
        {
            var blob = _configurationBlobDirectory.GetBlockBlobReference(HelperMethods.SHA256HashString(keyName));
            await blob.FetchAttributesAsync();
            return blob.Properties.LeaseState != LeaseState.Available;
        }

        private CloudBlockBlob GetBlob(string keyName) => _configurationBlobDirectory.GetBlockBlobReference(HelperMethods.SHA256HashString(keyName));
    }
}
