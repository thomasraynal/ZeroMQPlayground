using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface ICommand<TKey, TAgreggate> : IEvent<TKey, TAgreggate> where TAgreggate : IAggregate<TKey>
    {
    }
}
