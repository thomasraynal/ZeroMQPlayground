using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DynamicData;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class DynamicCache<TKey, TAggregate> : IDynamicCache<TKey, TAggregate>
        where TAggregate : IAggregate<TKey>
    {
        private SourceCache<TAggregate, TKey> _cache { get; }

        public DynamicCache()
        {
            _cache = new SourceCache<TAggregate, TKey>(selector => selector.Id);
        }

        public IObservableCache<TAggregate,TKey> Cache => _cache.AsObservableCache();

        public Task Connect(string endpoint)
        {

            //Task.Run( async() =>
            //{
            //    while (true)
            //    {
            //        _cache.

            //        await Task.Delay(1000);
            //    }


            //});

            return Task.CompletedTask;
        }
    }
}
