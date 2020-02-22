using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public class BlobDataStore<TDataType> : IDataStore<TDataType> where TDataType : IDataStoreCompatibleStructure
    {
        protected CloudBlobDirectory Directory { get; }

        protected string ObjectIdToName(Guid id)
        {
            return id.ToString();
        }

        protected Guid NameToObjectId(string name)
        {
            return Guid.Parse(name);
        }

        public BlobDataStore(CloudBlobDirectory directory)
        {
            Directory = directory;
        }

        public async Task Create(TDataType model)
        {
            await WriteObject(model.ObjectId, model);
        }

        public async Task Update(TDataType model)
        {
            await WriteObject(model.ObjectId, model);
        }

        public async Task<IList<Guid>> List()
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                BlobResultSegment response = await Directory.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results.Select(o => NameToObjectId(o.Uri.LocalPath)).ToList();
        }

        public Task Delete(Guid id)
        {
            return Directory.GetBlockBlobReference(ObjectIdToName(id)).DeleteIfExistsAsync();
        }

        public Task<TDataType> Get(Guid id)
        {
            return ReadObject(id);
        }

        public async Task<IList<TDataType>> Get(Func<TDataType, bool> selector)
        {
            IList<Guid> allObjects = await List();
            List<TDataType> allObjectsRead = allObjects.Select(o => ReadObject(o).Result).ToList();
            return allObjectsRead.Where(selector).ToList();
        }

        public async Task<IList<TDataType>> Get()
        {
            IList<Guid> allItems = await List();
            return allItems.Select(i => Get(i).Result).ToList();
        }

        private async Task WriteObject(Guid id, TDataType obj)
        {
            await Directory.GetBlockBlobReference(ObjectIdToName(id)).UploadTextAsync(JsonConvert.SerializeObject(obj));
        }

        private async Task<TDataType> ReadObject(Guid id)
        {
            return JsonConvert.DeserializeObject<TDataType>(await Directory.GetBlockBlobReference(ObjectIdToName(id)).DownloadTextAsync());
        }
    }
}
