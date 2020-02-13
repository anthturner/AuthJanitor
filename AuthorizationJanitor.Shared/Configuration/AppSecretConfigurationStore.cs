using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationJanitor.Shared.Configuration
{
    public class AppSecretConfigurationStore
    {
        private BlobServiceClient BlobServiceClient { get; }
        private BlobContainerClient BlobContainerClient { get; }

        public AppSecretConfigurationStore(string connectionString, string containerName)
        {
            BlobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient = BlobServiceClient.CreateBlobContainer(containerName)?.Value;
        }

        public async Task<IList<AppSecretConfiguration>> GetAll()
        {
            var items = new List<AppSecretConfiguration>();
            await foreach (var blobInfo in BlobContainerClient.GetBlobsAsync())
            {
                var blob = BlobContainerClient.GetBlobClient(blobInfo.Name);
                items.Add(await Get(blob));
            }
            return items;
        }

        public async Task<AppSecretConfiguration> Get(string keyName) => await Get(GetBlob(keyName));

        public async Task<AppSecretConfiguration> Get(BlobClient blob)
        {
            var blobDownload = await blob.DownloadAsync();

            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(blobDownload.Value.Content))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<AppSecretConfiguration>(jsonTextReader);
            }
        }

        public async Task Delete(string keyName)
        {
            var blob = GetBlob(keyName);
            await blob.DeleteIfExistsAsync();
        }

        public async Task Update(AppSecretConfiguration entity)
        {
            var blob = GetBlob(entity.AppSecretName);
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity));
            var ms = new MemoryStream(bytes);
            ms.Seek(0, SeekOrigin.Begin);
            await blob.UploadAsync(ms);
        }

        public async Task PerformLockedTask(string keyName, Func<Task> action)
        {
            var leaseClient = GetBlob(keyName).GetBlobLeaseClient();
            await leaseClient.AcquireAsync(BlobLeaseClient.InfiniteLeaseDuration);
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
                await leaseClient.ReleaseAsync();
            }
        }

        public async Task<bool> IsLocked(string keyName) =>
            (await GetBlob(keyName).GetPropertiesAsync()).Value.LeaseState != Azure.Storage.Blobs.Models.LeaseState.Available;

        private BlobClient GetBlob(string keyName) => BlobContainerClient.GetBlobClient(SHA256HashString(keyName));

        private static string SHA256HashString(string str)
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
    }
}
