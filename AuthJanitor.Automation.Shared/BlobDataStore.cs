using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public class AzureBlobDataStore<TDataType> : IDataStore<TDataType> where TDataType : IDataStoreCompatibleStructure
    {
        protected CloudBlockBlob Blob { get; }
        protected List<TDataType> _data = new List<TDataType>();

        public AzureBlobDataStore(CloudBlockBlob blob)
        {
            Blob = blob;
        }

        public async Task<IDataStore<TDataType>> CommitAsync()
        {
            await Blob.UploadTextAsync(JsonConvert.SerializeObject(_data));
            return this;
        }

        public bool ContainsId(Guid id) => _data.Any(d => d.ObjectId == id);

        public Task<bool> ContainsIdAsync(Guid id) => Task.FromResult(ContainsId(id));

        public void Create(TDataType model)
        {
            if (ContainsId(model.ObjectId))
                throw new InvalidOperationException("ID already exists!");
            _data.Add(model);
        }

        public Task CreateAsync(TDataType model) => Task.Run(() => Create(model));

        public void Delete(Guid id)
        {
            if (!ContainsId(id))
                throw new InvalidOperationException("ID does not exist!");
            _data.RemoveAll(d => d.ObjectId == id);
        }

        public Task DeleteAsync(Guid id) => Task.Run(() => Delete(id));

        public TDataType Get(Guid id)
        {
            if (!ContainsId(id))
                throw new InvalidOperationException("ID does not exist!");
            return _data.FirstOrDefault(d => d.ObjectId == id);
        }

        public List<TDataType> Get(Func<TDataType, bool> predicate)
        {
            return _data.Where(predicate).ToList();
        }

        public Task<TDataType> GetAsync(Guid id) => Task.FromResult(Get(id));

        public Task<List<TDataType>> GetAsync(Func<TDataType, bool> predicate) => Task.FromResult(Get(predicate));

        public TDataType GetOne(Func<TDataType, bool> predicate) => Get(predicate).FirstOrDefault();

        public Task<TDataType> GetOneAsync(Func<TDataType, bool> predicate) => Task.FromResult(GetOne(predicate));

        public async Task<IDataStore<TDataType>> InitializeAsync()
        {
            if (!await Blob.ExistsAsync())
            {
                await Blob.UploadTextAsync("[]");
                _data = new List<TDataType>();
            }
            else _data = JsonConvert.DeserializeObject<List<TDataType>>(await Blob.DownloadTextAsync());
            return this;
        }

        public List<TDataType> List() => new List<TDataType>(_data);

        public Task<List<TDataType>> ListAsync() => Task.FromResult(List());

        public void Update(TDataType model)
        {
            if (!ContainsId(model.ObjectId))
                throw new InvalidOperationException("ID does not exist!");
            Delete(model.ObjectId);
            Create(model);
        }

        public Task UpdateAsync(TDataType model) => Task.Run(() => Update(model));
    }
}
