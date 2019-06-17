using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DynamicData;

namespace ZeroMQPlayground.DynamicData
{
    public interface IClient<TKey,TAggregate> where TAggregate : IAggregate<TKey>
    {
        Task Connect(string endpoint);
        ISourceCache<TKey,TAggregate> Cache { get; }
    }
}
