using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Orleans.EventSourcing
{
    public class StoreProvider : IStoreProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public StoreProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEventStore GetEventStore(string name)
        {
            return _serviceProvider.GetServices<IEventStore>().Where(x => x.Name == name).LastOrDefault() ??
                throw new StoreNotFoundException($"EventStore by name `{name}` not configured.");
        }

        public IDataSerializer GetSerializer(string name)
        {
            return _serviceProvider.GetServices<IDataSerializer>().Where(x => x.Name == name).LastOrDefault() ??
                throw new StoreNotFoundException($"DataSerializer by name `{name}` not configured.");
        }

        public IStateStore GetStateStore(string name)
        {
            return _serviceProvider.GetServices<IStateStore>().Where(x => x.Name == name).LastOrDefault() ??
                throw new StoreNotFoundException($"StateStore by name `{name}` not configured.");
        }
    }
}
