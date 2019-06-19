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
        private string ToPublishersEndpoint = "tcp://localhost:8080";
        private string ToSubscribersEndpoint = "tcp://localhost:8181";
        private string HeartbeatEndpoint = "tcp://localhost:8282";
        private string StateOfTheWorldEndpoint = "tcp://localhost:8383";


        //[TearDown]
        //public void TearDown()
        //{
        //    NetMQConfig.Cleanup(false);
        //}


        //
        [Test]
        public async Task TestSubscribeToSubject()
        {
            //todo .NET COre MVC implem
            var eventIdProvider = new InMemoryEventIdProvider();
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);
          
            var eventCache = new InMemoryEventCache(eventIdProvider,eventSerializer);

            var router = new Broker(ToPublishersEndpoint, ToSubscribersEndpoint, StateOfTheWorldEndpoint, HeartbeatEndpoint, eventCache, serializer);

            var market1 = new Market("FxConnect", ToPublishersEndpoint, serializer,eventSerializer);
            var market2 = new Market("Harmony", ToPublishersEndpoint, serializer,eventSerializer);

            await router.Start();
            await market1.Start();
            await market2.Start();


            var cacheConfigurationEuroDol = new DynamicCacheConfiguration(ToSubscribersEndpoint, StateOfTheWorldEndpoint, HeartbeatEndpoint)
            {
                Subject = "EUR/USD"
            };

            var cacheConfigurationEuroDolFxConnect = new DynamicCacheConfiguration(ToSubscribersEndpoint, StateOfTheWorldEndpoint, HeartbeatEndpoint)
            {
                Subject = "EUR/USD.FxConnect"
            };

            var cacheEuroDol = new DynamicCache<string, CurrencyPair>(cacheConfigurationEuroDol, serializer, eventSerializer);
            var cacheEuroDolFxConnect = new DynamicCache<string, CurrencyPair>(cacheConfigurationEuroDolFxConnect, serializer, eventSerializer);

            await cacheEuroDol.Start();
            await cacheEuroDolFxConnect.Start();

            //wait for a substential event stream
            await Task.Delay(5000);

            // Assert.Greater(router.Cache.Count(), 1);

            var ccyPairsCacheEuroDol = cacheEuroDol.Items.SelectMany(item => item.AppliedEvents)
                                                         .Select(item => item.Subject)
                                                         .Distinct();

            // EUR/USD.FxConnect & EUR/USD.Harmony
            Assert.AreEqual(2, ccyPairsCacheEuroDol.Count());
            Assert.IsTrue(ccyPairsCacheEuroDol.All(subject => subject.EndsWith("FxConnect") || subject.EndsWith("Harmony")));
            Assert.IsTrue(ccyPairsCacheEuroDol.All(subject => subject.StartsWith(cacheConfigurationEuroDol.Subject)));

            var ccyPairsCacheEuroDolFxConnect = cacheEuroDolFxConnect.Items.SelectMany(item => item.AppliedEvents)
                                             .Select(item => item.Subject)
                                             .Distinct();

            // EUR/USD.FxConnect
            Assert.AreEqual(1, ccyPairsCacheEuroDolFxConnect.Count());
            Assert.AreEqual(cacheConfigurationEuroDolFxConnect.Subject, ccyPairsCacheEuroDolFxConnect.First());


            await Task.WhenAll(new[] { router.Stop(), market1.Stop(), market2.Stop(), cacheEuroDol.Stop(), cacheEuroDolFxConnect.Stop() });
        }

        [Test]
        public async Task TestSubscribeToEventFeed()
        {
            var eventIdProvider = new InMemoryEventIdProvider();
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);
            var eventCache = new InMemoryEventCache(eventIdProvider, eventSerializer);
     
            var router = new Broker(ToPublishersEndpoint, ToSubscribersEndpoint, StateOfTheWorldEndpoint, HeartbeatEndpoint, eventCache, serializer);
            var market1 = new Market("FxConnect", ToPublishersEndpoint, serializer, eventSerializer);
            var market2 = new Market("Harmony", ToPublishersEndpoint, serializer, eventSerializer);

            await router.Start();
            await market1.Start();
            await market2.Start();

            //create an event cache
            await Task.Delay(2000);

            //Assert.Greater(router.Cache.Count(), 0);

            var cacheConfiguration = new DynamicCacheConfiguration(ToSubscribersEndpoint, StateOfTheWorldEndpoint, HeartbeatEndpoint);

            var cache = new DynamicCache<string, CurrencyPair>(cacheConfiguration, serializer, eventSerializer);

            var counter = 0;

            var dispose = cache.AsObservableCache()
                               .Connect()
                               .Subscribe(_ =>
                               {
                                   counter++;
                               });

            await cache.Start();

            await Task.Delay(2000);

            // Assert.AreEqual(router.Cache.Count(), counter);
            Assert.AreEqual(cache.Items.SelectMany(item => item.AppliedEvents).Count(), counter);

            //fxconnext & harmony
            Assert.AreEqual(2, cache.Items
                                    .SelectMany(item => item.AppliedEvents)
                                    .Cast<ChangeCcyPairPrice>()
                                    .Select(ev => ev.Market)
                                    .Distinct()
                                    .Count());


            await Task.WhenAll(new[] { router.Stop(), market1.Stop(), market2.Stop(), cache.Stop() });

        }

        [Test]
        public void TestEventSubjectSerialization()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var changeCcyPairState = new ChangeCcyPairState()
            {
                AggregateId = "test",
                State = CcyPairState.Active,
                Market = "FxConnect"
            };

            var subject = eventSerializer.GetSubject(changeCcyPairState);
            Assert.AreEqual("test.Active.FxConnect", subject);

            changeCcyPairState = new ChangeCcyPairState()
            {
                AggregateId = "test",
                State = CcyPairState.Passive,
            };

            subject = eventSerializer.GetSubject(changeCcyPairState);
            Assert.AreEqual("test.Passive.*", subject);

            var changeCcyPairPrice = new ChangeCcyPairPrice(
                 ccyPairId: "test",
                 market: "market",
                 ask: 0.1,
                 bid: 0.1,
                 mid: 0.1,
                 spread: 0.02
             );

            subject = eventSerializer.GetSubject(changeCcyPairPrice);
            Assert.AreEqual("test.market", subject);


        }

        [Test]
        public void TestEventApply()
        {
            var ccyPair = new CurrencyPair()
            {
                Ask = 0.1,
                Bid = 0.1,
                Mid = 0.1,
                Spread = 0.02,
                State = CcyPairState.Active,
                Id = "EUR/USD"
            };

            var changeStateClose = new ChangeCcyPairState()
            {
                State = CcyPairState.Passive
            };

            ccyPair.Apply(changeStateClose);

            Assert.AreEqual(CcyPairState.Passive, ccyPair.State);
            Assert.AreEqual(1, ccyPair.AppliedEvents.Count());

            var changeStateOpen = new ChangeCcyPairState()
            {
                State = CcyPairState.Active
            };

            ccyPair.Apply(changeStateOpen as IEvent);

            Assert.AreEqual(CcyPairState.Active, ccyPair.State);
            Assert.AreEqual(2, ccyPair.AppliedEvents.Count());
        }

    }
}
