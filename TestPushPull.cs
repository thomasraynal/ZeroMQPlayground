using Microsoft.Extensions.Logging;
using NUnit.Framework;
using StructureMap;
using System;
using ZeroMQPlayground.PushPull;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground
{

    public class TestPushPullRegistry : Registry
    {
        public TestPushPullRegistry()
        {
            For<ILogger>().Use<Logger>();
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

            _bus.Subscribe<PeerRegisteredEvent>();
            _bus.Subscribe<MajorEventOccured>();
            _bus.Subscribe<MinorEventOccured>(ev => ev.Perimeter >= Perimeter.Infra);


        }
    }
}
