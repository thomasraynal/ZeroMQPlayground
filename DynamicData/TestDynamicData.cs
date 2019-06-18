using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.DynamicData.Domain;
using ZeroMQPlayground.DynamicData.Shared;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.DynamicData
{
    [TestFixture]
    public class TestDynamicData
    {
        private readonly Random _rand = new Random();
        private static readonly string[] CcyPairs = { "EUR/USD", "EUR/JPY", "EUR/GBP" };
        private CurrencyPair Next()
        {
            var mid = _rand.NextDouble() * 10;
            var spread = _rand.NextDouble() * 2;

            var price = new CurrencyPair()
            {
                Ask = mid + spread,
                Bid = mid - spread,
                Mid = mid,
                Spread = spread,
                State = CcyPairState.Active,
                Id =  CcyPairs[_rand.Next(0, 3)]
            };

            return price;
        }

        [Test]
        public async Task TestSubscribeToEventFeed()
        {

            var toPublishersEndpoint = "tcp://localhost:8080";
            var toSubscribersEndpoint = "tcp://localhost:8181";
            var heartbeatEndpoint = "tcp://localhost:8282";
            var stateOfTheWorldEndpoint = "tcp://localhost:8383";

            var cancel = new CancellationTokenSource();

            var router = new Broker(toPublishersEndpoint, toSubscribersEndpoint, stateOfTheWorldEndpoint, heartbeatEndpoint);
            var market = new Market(toPublishersEndpoint, cancel.Token);

            //create an event cache
            await Task.Delay(2000);

            Assert.Greater(router.Cache.Count(), 0);

            var cacheConfiguration = new DynamicCacheConfiguration(toSubscribersEndpoint, stateOfTheWorldEndpoint, heartbeatEndpoint);

            var cache = new DynamicCache<string, CurrencyPair>();

            var counter = 0;

            var dispose = cache.AsObservableCache()
                               .Connect()
                               .Subscribe(_ =>
                               {
                                   counter++;
                               });

            await cache.Connect(cacheConfiguration);

            await Task.Delay(2000);

            Assert.AreEqual(router.Cache.Values.Aggregate((l1, l2) => l1.Concat(l2).ToList()).Count(), counter);
            Assert.AreEqual(cache.Items.Select(item=> item.AppliedEvents).Aggregate((l1, l2) => l1.Concat(l2)).Count(), counter);

        }

        [Test]
        public void TestEventSerialization()
        {
            var @event = new ChangeCcyPairState()
            {
                AggregateId = "test",
                State = CcyPairState.Passive
            };

            var serializer = new EventSerializer();
            var subject = serializer.Serialize(@event);

            Assert.AreEqual("test.Passive", subject);

        }

        [Test]
        public void TestEventApply()
        {
            var stock = Next();

            var changeStateClose = new ChangeCcyPairState()
            {
                State = CcyPairState.Passive
            };

            stock.Apply(changeStateClose);

            Assert.AreEqual(CcyPairState.Passive, stock.State);
            Assert.AreEqual(1, stock.AppliedEvents.Count());

            var changeStateOpen = new ChangeCcyPairState()
            {
                State = CcyPairState.Active
            };

            stock.Apply(changeStateOpen as IEvent);

            Assert.AreEqual(CcyPairState.Active, stock.State);
            Assert.AreEqual(2, stock.AppliedEvents.Count());
        }

    }
}
