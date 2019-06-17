using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public interface IAggregate
    {
        IEnumerable<IEvent> AppliedEvents { get; }
        void Apply(IEvent @event);
    }

    public interface IAggregate<TKey> : IAggregate
    {
        TKey Id { get; set; }
        void Apply<TAggregate>(IEvent<TAggregate> @event) where TAggregate : IAggregate<TKey>;
    }
}
