using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventSourcing
{
    public interface IEventStore
    {
        Task<Slice> ReadAsync(string key, long version);
        Task<Slice> ReadUnpublishedAsync(string key);
        Task WriteAsync(string key, IEnumerable<StorableEvent> eventList);
        Task DeletePublishedAsync(string key, IEnumerable<StorableEvent> eventList);
    }
}
