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
                State = StockState.Open,
                Id =  CcyPairs[_rand.Next(0, 3)]
            };

            return price;
        }

        [Test]
        public async Task TestGenerateEvent()
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Objects,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

                settings.Converters.Add(new AbstractConverter<IEvent<string, CurrencyPair>, ChangeStockPrice>());

                return settings;
            };

            var toPublishersEndpoint = "tcp://localhost:8080";
            var toSubscribersEndpoint = "tcp://localhost:8181";

            var cancel = new CancellationTokenSource();

            var router = new Broker(toPublishersEndpoint, toSubscribersEndpoint, cancel.Token);
            var market = new Market(toPublishersEndpoint, cancel.Token);

            var cache = new DynamicCache<string, CurrencyPair>();

            var counter = 0;

            var dispose = cache.AsObservableCache()
                               .Connect()
                               .Subscribe(_ =>
                               {
                                   counter++;
                               });

            await cache.Connect(toSubscribersEndpoint, cancel.Token);

            await Task.Delay(2000);

            Assert.Greater(counter, 0);

        }

        [Test]
        public void TestEventApply()
        {
            var stock = Next();

            var changeStateClose = new ChangeStockState()
            {
                State = StockState.Close
            };

            stock.Apply(changeStateClose);

            Assert.AreEqual(StockState.Close, stock.State);
            Assert.AreEqual(1, stock.AppliedEvents.Count());

            var changeStateOpen = new ChangeStockState()
            {
                State = StockState.Open
            };

            stock.Apply(changeStateOpen as IEvent);

            Assert.AreEqual(StockState.Open, stock.State);
            Assert.AreEqual(2, stock.AppliedEvents.Count());
        }

    }
}
