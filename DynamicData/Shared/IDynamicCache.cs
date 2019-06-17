using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DynamicData;

namespace ZeroMQPlayground.DynamicData
{
    public interface IDynamicCache<TKey,TAggregate> where TAggregate : IAggregate<TKey>
    {
        Task Connect(string endpoint);
        IObservableCache<TAggregate,TKey> Cache { get; }
    }
}
