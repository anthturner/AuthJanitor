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
        Task<IList<Guid>> List();
        Task Delete(Guid id);
        Task<TDataType> Get(Guid id);
        Task<IList<TDataType>> Get(Func<TDataType, bool> predicate);
        Task<IList<TDataType>> Get();
    }
}
