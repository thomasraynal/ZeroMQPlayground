using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
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
            var pushStateUpdateEndpoint = "tcp://localhost:8080";
            var stateRequestEndpoint = "tcp://localhost:8181";
            var getUpdatesEndpoint = "tcp://localhost:8282";

            var brokerConfiguration = new BrokerConfiguration()
            {
                GetMarketUpdatesEndpoint = getUpdatesEndpoint,
                PushMarketUpdateEndpoint = pushStateUpdateEndpoint,
                GetMarketStateEndpoint = stateRequestEndpoint
            };

            var broker = new Broker(brokerConfiguration);

            broker.Start();

            var markets = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(ticker =>
             {
                 var coonfiguration = new MarketConfiguration()
                 {
                     GetMarketUpdatesEndpoint = getUpdatesEndpoint,
                     PushMarketUpdateEndpoint = pushStateUpdateEndpoint,
                     GetMarketStateEndpoint = stateRequestEndpoint,
                     Name = ticker,
                     RouterConnectionTimeout = TimeSpan.FromSeconds(5)
                 };

                 return new Market(coonfiguration);

             }).ToList();


            await Task.WhenAll(markets.Select(async market => await market.Start()));

            var msft = markets.First(market => market.Name == "MSFT");
            var goog = markets.First(market => market.Name == "GOOG");
            var fb = markets.First(market => market.Name == "FB");

            msft.Push();

            Assert.IsTrue(broker.MarketUpdates.Count == 1);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 1 &&
                market.Updates.First().Key == 0 &&
                market.Updates.First().Value.Asset == msft.Name));

            goog.Push();

            Assert.IsTrue(broker.MarketUpdates.Count == 2);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 2 &&
                market.Updates.ElementAt(1).Key == 1 &&
                market.Updates.ElementAt(1).Value.Asset == goog.Name));

            fb.Push();
            fb.Push();
            fb.Push();
            msft.Push();

            var amzConfig = new MarketConfiguration()
            {
                GetMarketUpdatesEndpoint = getUpdatesEndpoint,
                PushMarketUpdateEndpoint = pushStateUpdateEndpoint,
                GetMarketStateEndpoint = stateRequestEndpoint,
                Name = "AMZ",
                RouterConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            var amz = new Market(amzConfig);
            await amz.Start();

            //should have the last event of each ticker =>  1*MSFT, 1*GOOG, 1*FB
            Assert.IsTrue(amz.Updates.Count == 3);

            Assert.IsTrue(amz.Updates.ElementAt(0).Value.Asset == goog.Name);
            Assert.IsTrue(amz.Updates.ElementAt(0).Value.EventSequentialId == 1);

            Assert.IsTrue(amz.Updates.ElementAt(1).Value.Asset == fb.Name);
            Assert.IsTrue(amz.Updates.ElementAt(1).Value.EventSequentialId == 4);

            Assert.IsTrue(amz.Updates.ElementAt(2).Value.Asset == msft.Name);
            Assert.IsTrue(amz.Updates.ElementAt(2).Value.EventSequentialId == 5);
        }

    }
}
