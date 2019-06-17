using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class EventBase<TAgreggate> : IEvent<TAgreggate> where TAgreggate : IAggregate
    {
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
