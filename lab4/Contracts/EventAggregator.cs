using System.ComponentModel.Composition;

namespace Contracts
{
    [Export(typeof(IEventAggregator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SimpleEventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

        public void Publish<TEvent>(TEvent eventToPublish)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var value)) return;
            foreach (var handler in value.OfType<Action<TEvent>>())
            {
                handler(eventToPublish);
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptions.ContainsKey(eventType))
                _subscriptions[eventType] = [];

            _subscriptions[eventType].Add(handler);
        }
    }
}