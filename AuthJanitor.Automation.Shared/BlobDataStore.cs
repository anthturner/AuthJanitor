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
        protected CloudBlockBlob Blob { get; }
        protected List<TDataType> _data = new List<TDataType>();

        protected string ObjectIdToName(Guid id)
        {
            return id.ToString();
        }

        protected Guid NameToObjectId(string name)
        {
            return Guid.Parse(name);
        }

        public BlobDataStore(CloudBlockBlob blob)
        {
            Blob = blob;
        }

        public Task Create(TDataType model)
        {
            _data.Add(model);
            return Task.FromResult(true);
        }

        public Task Update(TDataType model)
        {
            _data.RemoveAll(d => d.ObjectId == model.ObjectId);
            _data.Add(model);
            return Task.FromResult(true);
        }

        public Task Delete(Guid id)
        {
            _data.RemoveAll(d => d.ObjectId == id);
            return Task.FromResult(true);
        }

        public Task<TDataType> Get(Guid id)
        {
            return Task.FromResult(_data.FirstOrDefault(d => d.ObjectId == id));
        }

        public Task<List<TDataType>> Get(Func<TDataType, bool> selector)
        {
            return Task.FromResult(_data.Where(selector).ToList());
        }

        public async Task<IDataStore<TDataType>> Initialize()
        {
            _data = JsonConvert.DeserializeObject<List<TDataType>>(await Blob.DownloadTextAsync());
            return this;
        }

        public async Task<IDataStore<TDataType>> Commit()
        {
            await Blob.UploadTextAsync(JsonConvert.SerializeObject(_data));
            return this;
        }

        public Task<List<TDataType>> List()
        {
            return Task.FromResult(_data);
        }
    }
}
