using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventSourcing
{
    public interface IStoreProvider
    {
        IEventStore GetEventStore(string name);
        IStateStore GetStateStore(string name);
        IDataSerializer GetSerializer(string name);
    }
}
