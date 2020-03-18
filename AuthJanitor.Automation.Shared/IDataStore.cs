using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public interface IDataStoreCompatibleStructure
    {
        Guid ObjectId { get; set; }
    }

    public interface IDataStore<TDataType> where TDataType : IDataStoreCompatibleStructure
    {
        Task<IDataStore<TDataType>> InitializeAsync();
        Task<IDataStore<TDataType>> CommitAsync();

        Task CreateAsync(TDataType model);
        void Create(TDataType model);

        Task UpdateAsync(TDataType model);
        void Update(TDataType model);

        Task<List<TDataType>> ListAsync();
        List<TDataType> List();

        Task DeleteAsync(Guid id);
        void Delete(Guid id);

        Task<bool> ContainsIdAsync(Guid id);
        bool ContainsId(Guid id);

        Task<TDataType> GetAsync(Guid id);
        TDataType Get(Guid id);
        
        Task<List<TDataType>> GetAsync(Func<TDataType, bool> predicate);
        List<TDataType> Get(Func<TDataType, bool> predicate);

        Task<TDataType> GetOneAsync(Func<TDataType, bool> predicate);
        TDataType GetOne(Func<TDataType, bool> predicate);
    }
}
