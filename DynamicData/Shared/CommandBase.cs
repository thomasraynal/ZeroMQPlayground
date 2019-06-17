using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class CommandBase<TKey, TAgreggate> : EventBase<TKey, TAgreggate> where TAgreggate : IAggregate<TKey>
    {
        protected CommandBase()
        {
        }

        protected CommandBase(TKey aggregateId) : base(aggregateId)
        {
        }
    }
}
