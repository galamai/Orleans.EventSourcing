using Orleans.EventSourcing;
using Orleans.EventSourcing.AzureStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureEventStore(this IServiceCollection services, string connectionString, string tableName = "EventStore", string storeName = "Default")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IEventStore>(new EventStore(storeName, connectionString, tableName));

            return services;
        }

        public static IServiceCollection AddAzureStateStore(this IServiceCollection services, string connectionString, string containerName = "states", string storeName = "Default")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IStateStore>(new StateStore(storeName, connectionString, containerName));

            return services;
        }
    }
}
