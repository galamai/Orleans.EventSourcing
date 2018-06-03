using System;
using System.Collections.Generic;
using System.Text;
using Orleans;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Orleans.Streams;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.EventSourcing
{
    public abstract class EventSourcedGrain<TState> : Grain where TState : class
    {
        private const byte DefaultSaveStateStep = 100;

        private readonly List<VersionedEvent> _uncommitedEvents = new List<VersionedEvent>();

        private string _key;
        private Reducer<TState> _reducer;
        private IStoreProvider _storeProvider;
        private IEventStore _eventStore;
        private IStateStore _stateStore;
        private IDataSerializer _dataSerializer;
        private IAsyncStream<VersionedEvent> _eventStream;

        protected uint SaveStateStep { get; set; } = DefaultSaveStateStep;
        protected bool PublishEventOnStream { get; set; } = true;
        protected bool SaveState { get; set; } = true;
        protected string EventStoreName { get; set; } = Constants.DefaultEventStoreName;
        protected string StateStoreName { get; set; } = Constants.DefaultStateStoreName;
        protected string DataSerializerName { get; set; } = Constants.DefaultDataSerializerName;
        protected string StreamProviderName { get; set; } = Constants.DefaultStreamProviderName;
        protected TState State { get; private set; }
        protected long Version { get; private set; } = -1;

        protected EventSourcedGrain(
            Reducer<TState> reducer = null,
            IStoreProvider storeProvider = null,
            TState initialState = null)
        {
            _reducer = reducer ?? Reducer.Reduce;
            State = initialState;
            _storeProvider = storeProvider;
        }

        public override async Task OnActivateAsync()
        {
            _key = GetKey();
            _storeProvider = _storeProvider ?? ServiceProvider.GetRequiredService<IStoreProvider>();
            _eventStore = _storeProvider.GetEventStore(EventStoreName);
            _stateStore = _storeProvider.GetStateStore(StateStoreName);
            _dataSerializer = _storeProvider.GetSerializer(DataSerializerName);
            _eventStream = GetStreamProvider(StreamProviderName)
                .GetStream<VersionedEvent>(this.GetPrimaryKey(), Constants.EventStreamNamespace);

            await Task.WhenAll(
                PublishUnpublishedEventsAsync(),
                ReadStateAsync(),
                base.OnActivateAsync());
        }

        protected void RaiseEvent(IEvent e)
        {
            var versionedEvent = new VersionedEvent(Version + 1, e);
            ApplyEvent(versionedEvent);
            _uncommitedEvents.Add(versionedEvent);
        }

        protected async Task WriteStateAsync()
        {
            if (!_uncommitedEvents.Any())
                return;

            await _eventStore.WriteAsync(_key, _uncommitedEvents.Select(ToStorableEvent));
            await PublishUnpublishedEventsAsync(_uncommitedEvents);

            if (SaveState &&
                Version >= SaveStateStep &&
                Version % SaveStateStep - _uncommitedEvents.Count < 0)
            {
                await _stateStore.WriteAsync(_key, new StorableState(Version, State.GetType().Name, _dataSerializer.Serialize(State)));
            }

            _uncommitedEvents.Clear();
        }

        private async Task ReadStateAsync()
        {
            var storableState = SaveState ? await _stateStore.ReadAsync(_key) : null;
            if (storableState != null)
            {
                Version = storableState.Version;
                State = _dataSerializer.Deserialize<TState>(storableState.Payload);
            }

            var hasMoreResults = false;
            do
            {
                var slice = await _eventStore.ReadAsync(_key, Version + 1);
                hasMoreResults = slice.HasMoreResults;
                if (slice.Events.Any())
                {
                    ApplyEvents(slice.Events.Select(ToVersionedEvent));
                }

            } while (hasMoreResults);
        }

        private async Task PublishUnpublishedEventsAsync()
        {
            var hasMoreResults = false;
            do
            {
                var slice = await _eventStore.ReadUnpublishedAsync(_key);
                hasMoreResults = slice.HasMoreResults;

                await PublishUnpublishedEventsAsync(slice.Events.Select(ToVersionedEvent));
            }
            while (hasMoreResults);
        }

        private async Task PublishUnpublishedEventsAsync(IEnumerable<VersionedEvent> unpublishedEvents)
        {
            if (unpublishedEvents.Any())
            {
                if (PublishEventOnStream)
                {
                    await _eventStream.OnNextBatchAsync(unpublishedEvents);
                }
                await _eventStore.DeletePublishedAsync(_key, unpublishedEvents.Select(x => x.Version));
            }
        }

        private void ApplyEvents(IEnumerable<VersionedEvent> events)
        {
            foreach (var e in events)
            {
                ApplyEvent(e);
            }
        }

        private void ApplyEvent(VersionedEvent e)
        {
            State = _reducer(State, e.Event);
            Version = e.Version;
        }

        private string GetKey()
        {
            var key = this.GetPrimaryKey(out string keyExt);
            return string.Join("_", new string[] { typeof(TState).Name, key.ToString(), keyExt }.Where(x => x != null))
                .Replace('/', '.')
                .Replace('+', '-');
        }

        private Func<VersionedEvent, StorableEvent> ToStorableEvent => (x) =>
            new StorableEvent(x.Version, x.Event.GetType().Name, _dataSerializer.Serialize(x.Event));

        private Func<StorableEvent, VersionedEvent> ToVersionedEvent => (x) =>
            new VersionedEvent(x.Version, _dataSerializer.Deserialize<IEvent>(x.Payload));
    }
}
