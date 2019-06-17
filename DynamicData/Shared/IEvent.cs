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

    public interface IEvent<TAgreggate> : IEvent where TAgreggate : IAggregate
    {
        void Apply(TAgreggate aggregate);
    }
}
