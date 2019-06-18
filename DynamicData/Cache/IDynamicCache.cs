using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public interface IDynamicCache<TKey,TAggregate> where TAggregate : IAggregate<TKey>
    {
        Task Connect(DynamicCacheConfiguration configuration);
        IObservableCache<TAggregate, TKey> AsObservableCache();
        IEnumerable<TAggregate> Items { get; }
        IObservable<bool> IsStale { get; }
        IObservable<bool> IsCaughtUp { get; }
    }
}
