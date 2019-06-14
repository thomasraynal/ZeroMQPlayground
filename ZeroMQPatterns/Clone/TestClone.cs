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
        //[TearDown]
        //public void TearDown()
        //{
        //    NetMQConfig.Cleanup(false);
        //}

        [Test]
        public async Task TestE2E()
        {
            var pushStateUpdateEndpoint = "tcp://localhost:8080";
            var stateRequestEndpoint = "tcp://localhost:8181";
            var getUpdatesEndpoint = "tcp://localhost:8282";

            var brokerConfiguration = new BrokerConfiguration()
            {
                GetUpdatesEndpoint = getUpdatesEndpoint,
                PushStateUpdateEndpoint = pushStateUpdateEndpoint,
                StateRequestEndpoint = stateRequestEndpoint
            };

            var markets = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(ticker =>
             {
                 var coonfiguration = new MarketConfiguration()
                 {
                     GetUpdatesEndpoint = getUpdatesEndpoint,
                     PushStateUpdateEndpoint = pushStateUpdateEndpoint,
                     StateRequestEndpoint = stateRequestEndpoint,
                     Name = $"Feed {ticker}",
                     RouterConnectionTimeout = TimeSpan.FromSeconds(5)
                 };

                 return new Market(coonfiguration);

             });

        }

    }
}
