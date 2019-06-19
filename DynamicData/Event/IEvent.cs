using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface IEvent
    {
        string EventId { get; }
        DateTime Timestamp { get; }
        string Subject { get; set; }
        Type EventType { get; }
        bool CanApply(Type type);
        void Apply(IAggregate aggregate);
        long Version { get; set; }
    }

    public interface IEvent<TKey, TAggregate> : IEvent where TAggregate : IAggregate<TKey>
    {
        TKey AggregateId { get; }
        void Apply(TAggregate aggregate);
    }
}
