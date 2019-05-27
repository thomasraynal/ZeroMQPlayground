using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Serialize.Linq.Serializers;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground
{

    public class TestPushPullRegistry : Registry
    {
        public TestPushPullRegistry()
        {
            For<ILogger>().Use<Logger>();
            For<IMessageDispatcher>().Use<MessageDispatcher>().Singleton();
            For<IDirectory>().Use<Directory>().Singleton();
            For<ITransport>().Use<Transport>().Singleton();
            For<IEventHandler<MajorEventOccured>>().Use<MajorEventOccuredHandler>();
            For<IEventHandler<MinorEventOccured>>().Use<MinorEventOccuredHandler>();

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new SubscriptionConverter());
            settings.Converters.Add(new AbstractConverter<Peer, IPeer>());
            //settings.Converters.Add(new AbstractConverter<TransportMessage, ITransportMessage>());

            For<JsonSerializerSettings>().Use(settings);

        }
    }

    [TestFixture]
    public class TestPushPull
    {
  

        [Test]
        public void TestExpressionSerialization()
        {
            var serializer = new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());

            var @event = new MajorEventOccured() { Severity = Severity.Error, Message = "Oh no!" };
            Expression<Func<MajorEventOccured, bool>> exp2 = (s) => true;

            var serialized = serializer.SerializeText(exp2);

            var exp = serializer.DeserializeText(serialized);
            var func = (exp as Expression<Func<MajorEventOccured, bool>>).Compile();

            Assert.True(func(@event));

        }

        [Test]
        public void TestSubscriptionSerialization()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new SubscriptionConverter());

            var @event = new MajorEventOccured() { Severity = Severity.Error, Message = "Oh no!" };

            var sub = new Subscription<MajorEventOccured>((ev) => ev.Severity >= Severity.Fatal);

            Assert.False(sub.CanHandle(@event));

            var json = JsonConvert.SerializeObject(sub, settings);
            sub = JsonConvert.DeserializeObject<Subscription<MajorEventOccured>>(json, settings);

            Assert.False(sub.CanHandle(@event));

        }

        [Test]
        public async Task TestStartPeer()
        {

            var configuration = new BusConfiguration()
            {
                PeerName = "London",
                Endpoint = "tcp://localhost:8585",
                DirectoryEndpoint = "tcp://localhost:8585",
                IsPeerDirectory = true
            };

            var bus = BusFactory.Create<TestPushPullRegistry>(configuration);
            var dispatcher = bus.Container.GetInstance<IMessageDispatcher>();

            var handler1 = bus.Container.GetInstance<IEventHandler<MajorEventOccured>>();
            var handler2 = bus.Container.GetInstance<IEventHandler<MinorEventOccured>>();

            bus.Register(handler1);
            bus.Register(handler2);

            await Task.Delay(500);

            Assert.AreEqual(2, dispatcher.HandledEvents.Count);

            Assert.IsTrue(dispatcher.HandledEvents.Any(ev=> ev.GetType() == typeof(PeerRegisterCommand)));
            Assert.IsTrue(dispatcher.HandledEvents.Any(ev => ev.GetType() == typeof(PeerRegisterCommandResult)));

            bus.Subscribe<MajorEventOccured>();
            bus.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);

            await Task.Delay(500);

            //2*2 PeerUpdatedEvent (PeerDirectory + Directory)  
            Assert.AreEqual(6, dispatcher.HandledEvents.Count);
            Assert.AreEqual(4, dispatcher.HandledEvents.Count(ev => ev.GetType() == typeof(PeerUpdatedEvent)));

            var directory = bus.Container.GetInstance<IDirectory>();
            var peerDirectory = bus.Container.GetInstance<IPeerDirectory>();

            Assert.AreEqual(1, directory.GetMatchedPeers(new MajorEventOccured()).Count());
            Assert.AreEqual(1, peerDirectory.StateOfTheWorld.Count());

            Assert.AreEqual(1, peerDirectory.StateOfTheWorld.First().Subscriptions.Count(sub => sub.EventType == typeof(MajorEventOccured)));
            Assert.AreEqual(1, peerDirectory.StateOfTheWorld.First().Subscriptions.Count(sub => sub.EventType == typeof(MinorEventOccured)));

            bus.Emit(new MajorEventOccured() { Severity = Severity.Fatal, Message = "Oh no!" });

            await Task.Delay(300);

            Assert.AreEqual(7, dispatcher.HandledEvents.Count);
            Assert.IsTrue(dispatcher.HandledEvents.Any(ev => ev.GetType() == typeof(MajorEventOccured)));

            bus.Emit(new MinorEventOccured() { Perimeter = Perimeter.Global, Message = "Oh no!" });

            await Task.Delay(100);

            Assert.AreEqual(8, dispatcher.HandledEvents.Count);

            Assert.IsTrue(dispatcher.HandledEvents.Any(ev => ev.GetType() == typeof(MinorEventOccured)));

            bus.Emit(new MinorEventOccured() { Perimeter = Perimeter.Business, Message = "Oh no!" });

            await Task.Delay(100);

            Assert.AreEqual(8, dispatcher.HandledEvents.Count);

            bus.Stop();
        }

        [Test]
        public async Task TestE2E()
        {
            var cancel = new CancellationTokenSource();
            IMessageDispatcher _dispatcher1 = null;
            IMessageDispatcher _dispatcher2 = null;

            IBus _peer2 = null;
            IBus _peer1 = null;

            var peer1EventCount = 0;
            var peer2EventCount = 0;

            new Task(async () =>
               {
                   var configuration = new BusConfiguration()
                   {
                       PeerName = "London",
                       Endpoint = "tcp://localhost:8181",
                       DirectoryEndpoint = "tcp://localhost:8181",
                       IsPeerDirectory = true
                   };

                   _peer1 = BusFactory.Create<TestPushPullRegistry>(configuration);

                   _dispatcher1 = _peer1.Container.GetInstance<IMessageDispatcher>();

                   var handler1 = _peer1.Container.GetInstance<IEventHandler<MajorEventOccured>>();
                   var handler2 = _peer1.Container.GetInstance<IEventHandler<MinorEventOccured>>();

                   _peer1.Register(handler1);
                   _peer1.Register(handler2);

                   _peer1.Subscribe<MajorEventOccured>();
                   _peer1.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);

                   while (!cancel.IsCancellationRequested)
                   {

                       _peer1.Emit(new MinorEventOccured() { Perimeter = Perimeter.Infra, Message = "Oh no!" });

                       peer1EventCount++;

                       await Task.Delay(500);

                   }


               }, cancel.Token).Start();

            await Task.Delay(2000);

            new Task(async () =>
            {
                var configuration = new BusConfiguration()
                {
                    PeerName = "Paris",
                    Endpoint = "tcp://localhost:8282",
                    DirectoryEndpoint = "tcp://localhost:8181"
                };

                _peer2 = BusFactory.Create<TestPushPullRegistry>(configuration);

                _dispatcher2 = _peer2.Container.GetInstance<IMessageDispatcher>();

                var handler1 = _peer2.Container.GetInstance<IEventHandler<MajorEventOccured>>();
                var handler2 = _peer2.Container.GetInstance<IEventHandler<MinorEventOccured>>();

                _peer2.Register(handler1);
                _peer2.Register(handler2);

                _peer2.Subscribe<MajorEventOccured>();
                _peer2.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);


                while (!cancel.IsCancellationRequested)
                {

                    _peer2.Emit(new MajorEventOccured() { Severity = Severity.Error, Message = "Oh no!" });

                    peer2EventCount++;

                    await Task.Delay(500);
                }


            }, cancel.Token).Start();

            await Task.Delay(5000);

            cancel.Cancel();

            _peer1.Stop();
            _peer2.Stop();

            Assert.IsNotNull(_dispatcher1);
            Assert.IsNotNull(_dispatcher2);

            //at least 1/3 events should be processed...
            Assert.IsTrue(_dispatcher1.HandledEvents.Where(ev => ev.GetType() == typeof(MajorEventOccured)).Count() > peer1EventCount / 3);
            Assert.IsTrue(_dispatcher1.HandledEvents.Where(ev => ev.GetType() == typeof(MinorEventOccured)).Count() > peer1EventCount / 3);
            Assert.IsTrue(_dispatcher2.HandledEvents.Where(ev => ev.GetType() == typeof(MajorEventOccured)).Count() > peer2EventCount / 3);
            Assert.IsTrue(_dispatcher2.HandledEvents.Where(ev => ev.GetType() == typeof(MinorEventOccured)).Count() > peer2EventCount / 3);
        }
    }
}
