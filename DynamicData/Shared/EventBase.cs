using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class EventBase<TKey,TAgreggate> : IEvent<TKey,TAgreggate> where TAgreggate : IAggregate<TKey>
    {
        protected EventBase()
        {
            EventDateTime = DateTime.Now;
        }

        protected EventBase(TKey aggregateId) : this()
        {
            AggregateId = aggregateId;

        }

        public TKey AggregateId { get; set; }

        public DateTime EventDateTime { get; set; }

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
