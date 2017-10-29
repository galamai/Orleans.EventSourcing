using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventSourcing
{
    public interface IStateStore
    {
        Task<(TState state, long version)> ReadAsync<TState>(string key);
        Task WriteAsync<TState>(string key, (TState state, long version) versionedState);
    }
}
