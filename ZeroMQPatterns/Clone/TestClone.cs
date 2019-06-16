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

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    [TestFixture]
    public class TestClone
    {
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        public async Task TestE2E()
        {

            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Objects,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

                settings.Converters.Add(new AbstractConverter<ISequenceItem<MarketStateDto>,
                                            DefaultSequenceItem<MarketStateDto>>());


                return settings;
            };

            var sendUpdatesEndpoint = "tcp://localhost:8080";
            var stateRequestEndpoint = "tcp://localhost:8181";
            var getUpdatesEndpoint = "tcp://localhost:8282";

            var brokerHeartbeatEndpoint = "tcp://localhost:8383";

            var brokerConfiguration = new BrokerConfiguration()
            {
                PublishUpdatesEndpoint = getUpdatesEndpoint,
                SubscribeToUpdatesEndpoint = sendUpdatesEndpoint,
                SendStateEndpoint = stateRequestEndpoint,
                HeartbeatEndpoint = brokerHeartbeatEndpoint
            };

            var broker = new Broker<MarketStateDto>(brokerConfiguration);

            broker.Start();

            var markets = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(ticker =>
             {
                 var coonfiguration = new ClientConfiguration()
                 {
                     SubscribeToUpdatesEndpoint = getUpdatesEndpoint,
                     PublishUpdateEndpoint = sendUpdatesEndpoint,
                     GetStateEndpoint = stateRequestEndpoint,
                     Name = ticker,
                     RouterConnectionTimeout = TimeSpan.FromSeconds(5)
                 };

                 return new Client<MarketStateDto>(coonfiguration);

             }).ToList();


            await Task.WhenAll(markets.Select(async market => await market.Start()));

            var msft = markets.First(market => market.Name == "MSFT");
            var goog = markets.First(market => market.Name == "GOOG");
            var fb = markets.First(market => market.Name == "FB");

            msft.Push(MarketStateDto.Random("MSFT"));

            await Task.Delay(200);

            Assert.IsTrue(broker.Updates.Count == 1);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 1 &&
                market.Updates.First().Key == 0 &&
                market.Updates.First().Value.UpdateDto.Subject == msft.Name));

            goog.Push(MarketStateDto.Random("GOOG"));

            Assert.IsTrue(broker.Updates.Count == 2);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 2 &&
                market.Updates.ElementAt(1).Key == 1 &&
                market.Updates.ElementAt(1).Value.UpdateDto.Subject == goog.Name));

            fb.Push(MarketStateDto.Random("FB"));
            fb.Push(MarketStateDto.Random("FB"));
            fb.Push(MarketStateDto.Random("FB"));
            msft.Push(MarketStateDto.Random("MSFT"));

            var amzConfig = new ClientConfiguration()
            {
                SubscribeToUpdatesEndpoint = getUpdatesEndpoint,
                PublishUpdateEndpoint = sendUpdatesEndpoint,
                GetStateEndpoint = stateRequestEndpoint,
                Name = "AMZ",
                RouterConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            var amz = new Client<MarketStateDto>(amzConfig);
            await amz.Start();

            //should have the last event of each ticker =>  1*MSFT, 1*GOOG, 1*FB
            Assert.IsTrue(amz.Updates.Count == 3);

            Assert.IsTrue(amz.Updates.ElementAt(0).Value.UpdateDto.Subject == goog.Name);
            Assert.IsTrue(amz.Updates.ElementAt(0).Value.Position == 1);

            Assert.IsTrue(amz.Updates.ElementAt(1).Value.UpdateDto.Subject == fb.Name);
            Assert.IsTrue(amz.Updates.ElementAt(1).Value.Position == 4);

            Assert.IsTrue(amz.Updates.ElementAt(2).Value.UpdateDto.Subject == msft.Name);
            Assert.IsTrue(amz.Updates.ElementAt(2).Value.Position == 5);
        }

    }
}
