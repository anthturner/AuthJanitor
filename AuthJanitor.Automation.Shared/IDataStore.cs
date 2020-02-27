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
        Task Create(TDataType model);
        Task Update(TDataType model);
        Task<List<TDataType>> List();
        Task Delete(Guid id);
        Task<TDataType> Get(Guid id);
        Task<List<TDataType>> Get(Func<TDataType, bool> predicate);

        Task<IDataStore<TDataType>> Initialize();
        Task<IDataStore<TDataType>> Commit();
    }
}
