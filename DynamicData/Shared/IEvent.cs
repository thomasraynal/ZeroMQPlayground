using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface IEvent
    {

        bool CanApply(Type type);
        void Apply(IAggregate aggregate);
    }

    public interface IEvent<TKey, TAgreggate> : IEvent where TAgreggate : IAggregate<TKey>
    {
        TKey AggregateId { get; }
        void Apply(TAgreggate aggregate);
    }
}
