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
        Task CreateAsync(TDataType model);

        Task UpdateAsync(TDataType model);

        Task<List<TDataType>> ListAsync();

        Task DeleteAsync(Guid id);

        Task<bool> ContainsIdAsync(Guid id);

        Task<TDataType> GetAsync(Guid id);
        
        Task<List<TDataType>> GetAsync(Func<TDataType, bool> predicate);

        Task<TDataType> GetOneAsync(Func<TDataType, bool> predicate);
    }
}
