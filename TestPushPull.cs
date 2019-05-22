using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Serialize.Linq.Serializers;
using StructureMap;
using System;
using System.Linq.Expressions;
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
        }
    }

    [TestFixture]
    public class TestPushPull
    {
        private BusConfiguration _configuration;
        private IBus _bus;

        private IEventHandler<MajorEventOccured> _handler1;
        private IEventHandler<MinorEventOccured> _handler2;

        [OneTimeSetUp]
        public void SetUp()
        {
            _configuration = new BusConfiguration()
            {
                PeerName = "London",
                Endpoint = "tcp://localhost:8080"
            };

            _bus = BusFactory.Create<TestPushPullRegistry>(_configuration);

            _handler1 = _bus.Container.GetInstance<IEventHandler<MajorEventOccured>>();
            _handler2 = _bus.Container.GetInstance<IEventHandler<MinorEventOccured>>();

            _bus.Register(_handler1);
            _bus.Register(_handler2);

            _bus.Subscribe<MajorEventOccured>();
            _bus.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);

        }

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
            var  func = (exp as Expression<Func<String, bool>>).Compile();

            Assert.True(func("abcdef"));
            Assert.False(func("ab"));

        }

        [Test]
        public void TestExpression2()
        {
            var serializer = new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());

            var @event = new MajorEventOccured() { Severity = Severity.Error, Message = "Oh no!" };
            Expression<Func<MajorEventOccured, bool>> exp2 = (s) =>  true;

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
        public async Task TestE2E()
        {
            var dispatcher = _bus.Container.GetInstance<IMessageDispatcher>();

            _bus.Emit(new MajorEventOccured() { Severity = Severity.Fatal, Message = "Oh no!" });

            await Task.Delay(200);    
        }
    }
}
