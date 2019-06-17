using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class TestCloneRegistry : Registry
    {
        public TestCloneRegistry()
        {
            For<IUniqueEventIdProvider>().Use<DefaultUniqueEventIdProvider>();
            For<ISequenceItem<MarketStateDto>>().Use<DefaultSequenceItem<MarketStateDto>>();

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

        }
    }

    [TestFixture]
    public class TestClone
    {
        
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }
        
        private string sendUpdatesEndpoint1 = "tcp://localhost:8080";
        private string stateRequestEndpoint1 = "tcp://localhost:8181";
        private string getUpdatesEndpoint1 = "tcp://localhost:8282";
        private string brokerHeartbeatEndpoint1 = "tcp://localhost:8383";

        private string sendUpdatesEndpoint2 = "tcp://localhost:8484";
        private string stateRequestEndpoint2 = "tcp://localhost:8585";
        private string getUpdatesEndpoint2 = "tcp://localhost:8686";
        private string brokerHeartbeatEndpoint2 = "tcp://localhost:8787";

        [Test]
        public async Task TestBrokerDisconnectAndCloneTakeCharge()
        {
            var container = new Container(new TestCloneRegistry());

            var broker1 = ActorFactory.GetBroker<MarketStateDto>
                (getUpdatesEndpoint1, sendUpdatesEndpoint1, stateRequestEndpoint1, brokerHeartbeatEndpoint1, container);


            var broker2 = ActorFactory.GetBroker<MarketStateDto>
                (getUpdatesEndpoint2, sendUpdatesEndpoint2, stateRequestEndpoint2, brokerHeartbeatEndpoint2, container);

            var brokerDescriptors = new List<BrokerDescriptor>()
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
                     };


            var marketsTasks = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(async ticker =>
            {
               return await ActorFactory.GetClient<MarketStateDto>(ticker, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000), brokerDescriptors, container);
            }).ToList();

            var markets = await Task.WhenAll(marketsTasks);

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

            await Task.Delay(1000);

            Assert.IsTrue(markets.All(market => market.Updates.Count == 4));

            broker2.Stop();

            foreach(var market in markets)
            {
                await market.Stop();
            }
        }

        [Test]
        public async Task TestE2E()
        {

            var container = new Container(new TestCloneRegistry());

            var broker = ActorFactory.GetBroker<MarketStateDto>
                (getUpdatesEndpoint1, sendUpdatesEndpoint1, stateRequestEndpoint1, brokerHeartbeatEndpoint1, container);

            var brokerDescriptors = new List<BrokerDescriptor>()
                     {
                         new BrokerDescriptor()
                         {
                             Name = "B1",
                             BrokerHeartbeatEndpoint = brokerHeartbeatEndpoint1,
                             GetStateEndpoint = stateRequestEndpoint1,
                             PublishUpdateEndpoint = sendUpdatesEndpoint1,
                             SubscribeToUpdatesEndpoint = getUpdatesEndpoint1
                         }
                     };


            var marketsTasks = new string[] { "AAPL", "MSFT", "GOOG", "FB" }.Select(async ticker =>
            {
                return await ActorFactory.GetClient<MarketStateDto>(
                    ticker, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000), brokerDescriptors, container);
            }).ToList();

            var markets = await Task.WhenAll(marketsTasks);

            var msft = markets.First(market => market.Name == "MSFT");
            var goog = markets.First(market => market.Name == "GOOG");
            var fb = markets.First(market => market.Name == "FB");

            msft.Push(MarketStateDto.Random("MSFT"));

            //should not have to wait...
            await Task.Delay(1000);

            Assert.IsTrue(broker.Updates.Count == 1);

            Assert.IsTrue(markets.All(
                market => market.Updates.Count == 1 &&
                market.Updates.First().Key == 0 &&
                market.Updates.First().Value.UpdateDto.Subject == msft.Name));

            goog.Push(MarketStateDto.Random("GOOG"));

            await Task.Delay(100);

            Assert.IsTrue(broker.Updates.Count == 2);

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

            broker.Stop();

            await amz.Stop();

            foreach (var market in markets)
            {
                await market.Stop();
            }
        }

    }
}
