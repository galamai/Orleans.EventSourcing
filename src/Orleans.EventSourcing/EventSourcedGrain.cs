using System;
using System.Collections.Generic;
using System.Text;
using Orleans;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Orleans.Streams;

namespace Orleans.EventSourcing
{
    public abstract class EventSourcedGrain<TState> : Grain where TState : class
    {
        private const byte DefaultSaveStateStep = 100;

        private readonly Reducer<TState> _reducer;
        private readonly IEventStore _eventStore;
        private readonly IStateStore _stateStore;
        private readonly IDataSerializer _dataSerializer;
        private readonly List<VersionedEvent> _uncommitedEvents = new List<VersionedEvent>();

        private string _key;
        private IAsyncStream<VersionedEvent> _eventStream;

        protected byte SaveStateStep { get; set; } = DefaultSaveStateStep;
        protected string EventStoreName { get; set; } = "Default";
        protected string StateStoreName { get; set; } = "Default";
        protected string DataSerializerName { get; set; } = "Default";
        protected TState State { get; private set; }
        protected long Version { get; private set; } = -1;


        public EventSourcedGrain(
            Reducer<TState> reducer,
            IStoreProvider storeProvider,
            TState initialState = null)
        {
            if (storeProvider == null)
                throw new ArgumentNullException(nameof(storeProvider));

            _reducer = reducer;
            _eventStore = storeProvider.GetEventStore(EventStoreName);
            _stateStore = storeProvider.GetStateStore(StateStoreName);
            _dataSerializer = storeProvider.GetSerializer(DataSerializerName);
            State = initialState;
        }

        public override async Task OnActivateAsync()
        {
            _key = GetKey();
            _eventStream = GetStreamProvider("Default").GetStream<VersionedEvent>(this.GetPrimaryKey(), "Events");

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

            if (Version >= SaveStateStep &&
                Version % SaveStateStep - _uncommitedEvents.Count < 0)
            {
                await _stateStore.WriteAsync(_key, new StorableState(Version, State.GetType().Name, _dataSerializer.Serialize(State)));
            }

            _uncommitedEvents.Clear();
        }

        private async Task ReadStateAsync()
        {
            var storableState = await _stateStore.ReadAsync(_key);
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
                await _eventStream.OnNextBatchAsync(unpublishedEvents);
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
