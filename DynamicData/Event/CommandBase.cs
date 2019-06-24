using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class CommandBase<TKey, TAggregate> : EventBase<TKey, TAggregate> where TAggregate : IAggregate<TKey>
    {
        protected CommandBase()
        {
        }

        protected CommandBase(TKey aggregateId) : base(aggregateId)
        {
        }
    }
}
