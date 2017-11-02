using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    [Serializable]
    public class VersionedEvent
    {
        public long Version { get; }
        public IEvent Event { get; }

        public VersionedEvent(long version, IEvent @event)
        {
            Version = version;
            Event = @event;
        }
    }
}
