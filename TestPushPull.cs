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
using ZeroMQPlayground.Shared;

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

        private String Serialize(Expression<Func<string, bool>> exp)
        {
            var serializer = new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());
            return serializer.SerializeText(exp);
        }

        private Expression DeSerialize(String exp)
        {
            var serializer = new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());
            return serializer.DeserializeText(exp);
        }

        [Test]
        public void TestExpression()
        {
            Expression<Func<string, bool>> exp2 = (s) => s.Length > 3;

            var serialized = Serialize((s) => s.Length > 3);
            var exp = DeSerialize(serialized);
            var func = (exp as Expression<Func<String, bool>>).Compile();

            Assert.True(func("abcdef"));
            Assert.False(func("ab"));

        }

        [Test]
        public void TestExpression2()
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
        public async Task TestOnePeer()
        {

            var configuration = new BusConfiguration()
            {
                PeerName = "London",
                Endpoint = "tcp://localhost:8181",
                DirectoryEndpoint = "tcp://localhost:8181",
            };

            var bus = BusFactory.Create<TestPushPullRegistry>(configuration);

            var handler1 = bus.Container.GetInstance<IEventHandler<MajorEventOccured>>();
            var handler2 = bus.Container.GetInstance<IEventHandler<MinorEventOccured>>();

            bus.Register(handler1);
            bus.Register(handler2);

            bus.Subscribe<MajorEventOccured>();
            bus.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);

            var dispatcher = bus.Container.GetInstance<IMessageDispatcher>();

            bus.Emit(new MajorEventOccured() { Severity = Severity.Fatal, Message = "Oh no!" });

            await Task.Delay(300);

            Assert.AreEqual(3, dispatcher.HandledEvents.Count);

            //subscription
            Assert.IsTrue(dispatcher.HandledEvents.Any(ev => ev.GetType() == typeof(PeerUpdatedEvent)));
            //business event
            Assert.IsTrue(dispatcher.HandledEvents.Any(ev => ev.GetType() == typeof(MajorEventOccured)));


            bus.Emit(new MinorEventOccured() { Perimeter = Perimeter.Global, Message = "Oh no!" });

            await Task.Delay(100);

            Assert.AreEqual(4, dispatcher.HandledEvents.Count);

            Assert.IsTrue(dispatcher.HandledEvents.Any(ev => ev.GetType() == typeof(MinorEventOccured)));

            bus.Emit(new MinorEventOccured() { Perimeter = Perimeter.Business, Message = "Oh no!" });

            await Task.Delay(100);

            Assert.AreEqual(4, dispatcher.HandledEvents.Count);
        }

        [Test]
        public async Task TestE2E()
        {
            var cancel = new CancellationTokenSource();
            IMessageDispatcher _dispatcher1;
            IMessageDispatcher _dispatcher2;

            new Task(async () =>
               {
                   var configuration = new BusConfiguration()
                   {
                       PeerName = "London",
                       Endpoint = "tcp://localhost:8181"
                   };

                   var bus = BusFactory.Create<TestPushPullRegistry>(configuration);

                   _dispatcher1 = bus.Container.GetInstance<IMessageDispatcher>();

                   var handler1 = bus.Container.GetInstance<IEventHandler<MajorEventOccured>>();
                   var handler2 = bus.Container.GetInstance<IEventHandler<MinorEventOccured>>();

                   bus.Register(handler1);
                   bus.Register(handler2);

                   bus.Subscribe<MajorEventOccured>();
                   bus.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);

                   while (!cancel.IsCancellationRequested)
                   {
                       await Task.Delay(500);

                       bus.Emit(new MinorEventOccured() { Perimeter = Perimeter.Infra, Message = "Oh no!" });
                   }


               }, cancel.Token).Start();

            await Task.Delay(1000);

            new Task(async () =>
            {
                var configuration = new BusConfiguration()
                {
                    PeerName = "Paris",
                    Endpoint = "tcp://localhost:8282"
                };

                var bus = BusFactory.Create<TestPushPullRegistry>(configuration);

                _dispatcher2 = bus.Container.GetInstance<IMessageDispatcher>();

                var handler1 = bus.Container.GetInstance<IEventHandler<MajorEventOccured>>();
                var handler2 = bus.Container.GetInstance<IEventHandler<MinorEventOccured>>();

                bus.Register(handler1);
                bus.Register(handler2);

                bus.Subscribe<MajorEventOccured>();
                bus.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);


                while (!cancel.IsCancellationRequested)
                {
                    await Task.Delay(500);

                    bus.Emit(new MajorEventOccured() { Severity = Severity.Error, Message = "Oh no!" });
                }


            }, cancel.Token).Start();

            await Task.Delay(5000);

        }
    }
}
