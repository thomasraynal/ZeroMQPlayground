using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class EventBase<TKey,TAgreggate> : IEvent<TKey,TAgreggate> where TAgreggate : IAggregate<TKey>
    {
        protected EventBase()
        {
            Timestamp = DateTime.Now;
            EventType = this.GetType();
        }

        protected EventBase(TKey aggregateId) : this()
        {
            AggregateId = aggregateId;
        }

        [RoutingPosition(0)]
        public TKey AggregateId { get; set; }

        public DateTime Timestamp { get; set; }

        public Type EventType { get ; set; }

        public string Subject { get;  set; }

        public abstract void Apply(TAgreggate aggregate);

        public void Apply(IAggregate aggregate)
        {
            Apply((dynamic)aggregate);
        }

        public bool CanApply(Type type)
        {
            return type == typeof(TAgreggate);
        }

        public void Validate()
        {
            var serializer = new EventSerializer();
            Subject = serializer.Serialize(this);
        }
    }
}
