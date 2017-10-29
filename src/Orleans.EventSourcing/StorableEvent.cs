using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public class StorableEvent
    {
        public long Version { get; }
        public IEvent Event { get; }
        
        public StorableEvent(long version, IEvent @event)
        {
            Version = version;
            Event = @event;
        }
    }
}
