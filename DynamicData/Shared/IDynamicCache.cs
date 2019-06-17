using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;

namespace ZeroMQPlayground.DynamicData
{
    public interface IDynamicCache<TKey,TAggregate> where TAggregate : IAggregate<TKey>
    {
        Task Connect(string endpoint, CancellationToken token);
        IObservableCache<TAggregate, TKey> AsObservableCache();
        IObservable<bool> IsStale { get; }
        IObservable<bool> IsCaughtUp { get; }
    }
}
