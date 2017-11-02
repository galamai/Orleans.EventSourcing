using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public interface IDataSerializer
    {
        string Name { get; }
        byte[] Serialize(object value);
        T Deserialize<T>(byte[] payload);
    }
}
