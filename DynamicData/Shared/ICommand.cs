using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface ICommand<TAgreggate> : IEvent<TAgreggate> 
        where TAgreggate : IAggregate
    {
    }
}
