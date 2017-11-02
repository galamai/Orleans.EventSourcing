using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventSourcing
{
    public interface IStateStore
    {
        string Name { get; }
        Task<StorableState> ReadAsync(string key);
        Task WriteAsync(string key, StorableState storableState);
    }
}
