using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public class StorableState
    {
        public long Version { get; }
        public string Type { get; }
        public byte[] Payload { get; }
        
        public StorableState(long version, string type, byte[] payload)
        {
            Version = version;
            Type = type;
            Payload = payload;
        }
    }
}
