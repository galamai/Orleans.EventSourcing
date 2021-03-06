﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public class StorableEvent
    {
        public long Version { get; }
        public string Type { get; }
        public byte[] Payload { get; }

        public StorableEvent(long version, string type, byte[] payload)
        {
            Version = version;
            Type = type;
            Payload = payload;
        }
    }
}
