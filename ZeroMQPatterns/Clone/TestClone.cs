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
        //[TearDown]
        //public void TearDown()
        //{
        //    NetMQConfig.Cleanup(false);
        //}

       use container

        private string sendUpdatesEndpoint1 = "tcp://localhost:8080";
        private string stateRequestEndpoint1 = "tcp://localhost:8181";
        private string getUpdatesEndpoint1 = "tcp://localhost:8282";
        private string brokerHeartbeatEndpoint1 = "tcp://localhost:8383";

        private string sendUpdatesEndpoint2 = "tcp://localhost:8484";
        private string stateRequestEndpoint2 = "tcp://localhost:8585";
        private string getUpdatesEndpoint2 = "tcp://localhost:8686";
        private string brokerHeartbeatEndpoint2 = "tcp://localhost:8787";

        [Test]
        public async Task TestBrokerDisconnect()
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

            var broker1 = new Broker<MarketStateDto>(new BrokerConfiguration()
            {
                PublishUpdatesEndpoint = getUpdatesEndpoint1,
                SubscribeToUpdatesEndpoint = sendUpdatesEndpoint1,
                SendStateEndpoint = stateRequestEndpoint1,
                HeartbeatEndpoint = brokerHeartbeatEndpoint1
            });

            var broker2 = new Broker<MarketStateDto>(new BrokerConfiguration()
            {
                PublishUpdatesEndpoint = getUpdatesEndpoint2,
                SubscribeToUpdatesEndpoint = sendUpdatesEndpoint2,
                SendStateEndpoint = stateRequestEndpoint2,
                HeartbeatEndpoint = brokerHeartbeatEndpoint2
            });

            broker1.Start();
            broker2.Start();

            var markets = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(ticker =>
            {
                var coonfiguration = new ClientConfiguration()
                {

                    BrokerDescriptors = new List<BrokerDescriptor>()
                     {
                         new BrokerDescriptor()
                         {
                             Name = "B1",
                             BrokerHeartbeatEndpoint = brokerHeartbeatEndpoint1,
                             GetStateEndpoint = stateRequestEndpoint1,
                             PublishUpdateEndpoint = sendUpdatesEndpoint1,
                             SubscribeToUpdatesEndpoint = getUpdatesEndpoint1
                         },
                         new BrokerDescriptor()
                         {
                             Name = "B2",
                             BrokerHeartbeatEndpoint = brokerHeartbeatEndpoint2,
                             GetStateEndpoint = stateRequestEndpoint2,
                             PublishUpdateEndpoint = sendUpdatesEndpoint2,
                             SubscribeToUpdatesEndpoint = getUpdatesEndpoint2
                         }
                     },

                    Name = ticker,
                    HearbeatDelay = TimeSpan.FromMilliseconds(500),
                    HearbeatMaxDelay = TimeSpan.FromMilliseconds(1000)

                };

                return new Client<MarketStateDto>(coonfiguration);

            }).ToList();

            await Task.WhenAll(markets.Select(async market => await market.Start()));

            var msft = markets.First(market => market.Name == "MSFT");
            var goog = markets.First(market => market.Name == "GOOG");
            var fb = markets.First(market => market.Name == "FB");
            var appl = markets.First(market => market.Name == "AAPL");


            Assert.IsTrue(markets.All(market => market.Broker.Name == "B1"));

            fb.Push(MarketStateDto.Random("FB"));
            msft.Push(MarketStateDto.Random("MSFT"));

            //should not have to wait...
            await Task.Delay(1000);

            Assert.IsTrue(markets.All(market => market.Updates.Count == 2));

            broker1.Stop();

            await Task.Delay(2000);

            Assert.IsTrue(markets.All(market => market.Broker != null && market.Broker.Name == "B2"));

            fb.Push(MarketStateDto.Random("FB"));
            msft.Push(MarketStateDto.Random("MSFT"));

            await Task.Delay(100);

            Assert.IsTrue(markets.All(market => market.Updates.Count == 4));

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


            var broker1 = new Broker<MarketStateDto>(new BrokerConfiguration()
            {
                PublishUpdatesEndpoint = getUpdatesEndpoint1,
                SubscribeToUpdatesEndpoint = sendUpdatesEndpoint1,
                SendStateEndpoint = stateRequestEndpoint1,
                HeartbeatEndpoint = brokerHeartbeatEndpoint1
            });


            broker1.Start();

            var markets = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(ticker =>
             {
                 var coonfiguration = new ClientConfiguration()
                 {
  
                     BrokerDescriptors = new List<BrokerDescriptor>()
                     {
                         new BrokerDescriptor()
                         {
                             Name = "B1",
                             BrokerHeartbeatEndpoint = brokerHeartbeatEndpoint1,
                             GetStateEndpoint = stateRequestEndpoint1,
                             PublishUpdateEndpoint = sendUpdatesEndpoint1,
                             SubscribeToUpdatesEndpoint = getUpdatesEndpoint1
                         }
                     },

                     Name = ticker,
                     HearbeatDelay = TimeSpan.FromMilliseconds(250),
                     HearbeatMaxDelay = TimeSpan.FromSeconds(1)
                
                 };

                 return new Client<MarketStateDto>(coonfiguration);

             }).ToList();


            await Task.WhenAll(markets.Select(async market => await market.Start()));

            var msft = markets.First(market => market.Name == "MSFT");
            var goog = markets.First(market => market.Name == "GOOG");
            var fb = markets.First(market => market.Name == "FB");

            msft.Push(MarketStateDto.Random("MSFT"));

            //should not have to wait...
            await Task.Delay(1000);

            Assert.IsTrue(broker1.Updates.Count == 1);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 1 &&
                market.Updates.First().Key == 0 &&
                market.Updates.First().Value.UpdateDto.Subject == msft.Name));

            goog.Push(MarketStateDto.Random("GOOG"));

            await Task.Delay(100);

            Assert.IsTrue(broker1.Updates.Count == 2);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 2 &&
                market.Updates.ElementAt(1).Key == 1 &&
                market.Updates.ElementAt(1).Value.UpdateDto.Subject == goog.Name));

            fb.Push(MarketStateDto.Random("FB"));
            fb.Push(MarketStateDto.Random("FB"));
            fb.Push(MarketStateDto.Random("FB"));
            msft.Push(MarketStateDto.Random("MSFT"));

            await Task.Delay(1000);

            var amzConfig = new ClientConfiguration()
            {
                BrokerDescriptors = new List<BrokerDescriptor>()
                     {
                         new BrokerDescriptor()
                         {
                             Name = "B1",
                             BrokerHeartbeatEndpoint = brokerHeartbeatEndpoint1,
                             GetStateEndpoint = stateRequestEndpoint1,
                             PublishUpdateEndpoint = sendUpdatesEndpoint1,
                             SubscribeToUpdatesEndpoint = getUpdatesEndpoint1
                         }
                     },

                Name = "AMZ",
                HearbeatDelay = TimeSpan.FromMilliseconds(250),
                HearbeatMaxDelay = TimeSpan.FromSeconds(1)
            };

            var amz = new Client<MarketStateDto>(amzConfig);
            await amz.Start();

            Assert.IsTrue(amz.Updates.Count == 6);
        }

    }
}
