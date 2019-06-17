using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public abstract class CommandBase<TAgreggate> : EventBase<TAgreggate> where TAgreggate : IAggregate
    {
    }
}
