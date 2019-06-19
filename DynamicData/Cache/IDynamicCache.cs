using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public interface IDynamicCache<TKey,TAggregate> : IActor where TAggregate : IAggregate<TKey>
    {
        IObservableCache<TAggregate, TKey> AsObservableCache();
        IEnumerable<TAggregate> Items { get; }
        IObservable<DynamicCacheState> State { get; }
    }
}
