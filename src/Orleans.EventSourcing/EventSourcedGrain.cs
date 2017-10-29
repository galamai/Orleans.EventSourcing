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
    public abstract class EventSourcedGrain<TState> : Grain, IGrainWithState<TState> where TState : class
    {
        private const byte DefaultSaveStateStep = 100;

        private readonly Reducer<TState> _reducer;
        private readonly IEventStore _eventStore;
        private readonly IStateStore _stateStore;
        private readonly List<StorableEvent> _uncommitedEvents = new List<StorableEvent>();

        private string _key;
        private IAsyncStream<IEvent> _eventStream;

        protected byte SaveStateStep { get; set; } = DefaultSaveStateStep;
        protected TState State { get; private set; }
        protected long Version { get; private set; } = -1;

        public EventSourcedGrain(Reducer<TState> reducer, IEventStore eventStore, IStateStore stateStore, TState initialState = null)
        {
            _reducer = reducer;
            _eventStore = eventStore;
            _stateStore = stateStore;
            State = initialState;
        }

        public override async Task OnActivateAsync()
        {
            _key = GetKey();

            await Task.WhenAll(
                PublishUnpublishedEventsAsync(),
                ReadStateAsync(),
                base.OnActivateAsync());

            _eventStream = GetStreamProvider("Default").GetStream<IEvent>(this.GetPrimaryKey(), "Events");
        }

        protected void RaiseEvent(IEvent e)
        {
            var storableEvent = new StorableEvent(Version, e);
            ApplyEvent(storableEvent);
            _uncommitedEvents.Add(storableEvent);
        }

        protected async Task WriteStateAsync()
        {
            if (!_uncommitedEvents.Any())
                return;

            await _eventStore.WriteAsync(_key, _uncommitedEvents);
            await PublishUnpublishedEventsAsync(_uncommitedEvents);

            if (Version >= SaveStateStep &&
                Version % SaveStateStep - _uncommitedEvents.Count < 0)
            {
                await _stateStore.WriteAsync(_key, (State, Version));
            }

            _uncommitedEvents.Clear();
        }

        private async Task ReadStateAsync()
        {
            var versionedState = await _stateStore.ReadAsync<TState>(_key);
            Version = versionedState.version;
            State = versionedState.state ?? State;

            var hasMoreResults = false;
            do
            {
                var slice = await _eventStore.ReadAsync(_key, Version);
                hasMoreResults = slice.HasMoreResults;
                if (slice.Events.Any())
                {
                    ApplyEvents(slice.Events);
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

                await PublishUnpublishedEventsAsync(slice.Events);
            }
            while (hasMoreResults);
        }

        private async Task PublishUnpublishedEventsAsync(IEnumerable<StorableEvent> unpublishedEvents)
        {
            if (unpublishedEvents.Any())
            {
                await _eventStream.OnNextBatchAsync(unpublishedEvents.Select(x => x.Event));
                await _eventStore.DeletePublishedAsync(_key, unpublishedEvents);
            }
        }

        private void ApplyEvents(IEnumerable<StorableEvent> events)
        {
            foreach (var e in events)
            {
                ApplyEvent(e);
            }
        }

        private void ApplyEvent(StorableEvent e)
        {
            State = _reducer(State, e.Event);
            Version = e.Version + 1;
        }

        private string GetKey()
        {
            var key = this.GetPrimaryKey(out string keyExt);
            return string.Join("_", new string[] { typeof(TState).Name, key.ToString(), keyExt }.Where(x => x != null))
                .Replace('/', '.')
                .Replace('+', '-');
        }

        public Task<TState> GetStateAsync()
        {
            return Task.FromResult(State);
        }
    }
}
