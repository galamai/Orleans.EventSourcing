using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventSourcing
{
    public interface IEventStore
    {
        string Name { get; }
        Task<Slice> ReadAsync(string key, long version);
        Task<Slice> ReadUnpublishedAsync(string key);
        Task WriteAsync(string key, IEnumerable<StorableEvent> eventList);
        Task DeletePublishedAsync(string key, IEnumerable<long> versionList);
    }
}
