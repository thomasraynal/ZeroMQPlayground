using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class AggregateBase<TKey> : IAggregate<TKey>
    {
        public AggregateBase()
        {
            AppliedEvents = new List<IEvent>();
        }

        public TKey Id { get; set; }

        public IEnumerable<IEvent> AppliedEvents { get; }

        public void Apply(IEvent @event)
        {
            if (!@event.CanApply(GetType())) throw new Exception($"cant apply to {this.GetType()}");

            @event.Apply((dynamic)this);
        }

        public void Apply<TAggregate>(IEvent<TAggregate> @event) where TAggregate : IAggregate<TKey>
        {
            @event.Apply((dynamic)this);
        }
    }
}
