using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class EventBase<TKey, TAgreggate> : IEvent<TKey, TAgreggate> where TAgreggate : IAggregate<TKey>
    {
        protected EventBase()
        {
            EventType = this.GetType();
        }

        protected EventBase(TKey aggregateId) : this()
        {
            EventStreamId = aggregateId;
        }

        public TKey EventStreamId { get; set; }

        public Type EventType { get; set; }

        public string Subject { get; set; }

        public long Version { get; set; }

        public string EventId => $"{EventStreamId}.{Version}";

        public abstract void Apply(TAgreggate aggregate);

        public void Apply(IAggregate aggregate)
        {
            Apply((dynamic)aggregate);
        }

        public bool CanApply(Type type)
        {
            return type == typeof(TAgreggate);
        }
    }
}
