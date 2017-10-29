using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Orleans.EventSourcing
{
    public class Slice
    {
        public IEnumerable<StorableEvent> Events { get; }
        public bool HasMoreResults { get; }
        
        public Slice(IEnumerable<StorableEvent> events, bool hasMoreResults)
        {
            Events = events.ToList();
            HasMoreResults = hasMoreResults;
        }
    }
}
