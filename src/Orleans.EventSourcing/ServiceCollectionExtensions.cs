using Orleans.EventSourcing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventSourcingGrain(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IStoreProvider, StoreProvider>();
            services.AddSingleton<IDataSerializer>(new JsonDataSerializer("Default"));

            return services;
        }
    }
}
