using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public static class BusFactory
    {
        public static IBus Create<TRegistry>(BusConfiguration configuration) where TRegistry : Registry
        {
            var container = new Container((conf) => conf.AddRegistry(Activator.CreateInstance<TRegistry>()));
            var self = new Peer(Guid.NewGuid(), configuration.PeerName, configuration.Endpoint);
            var bus = new Bus(container, self);

            container.Configure(conf => conf.For<IBus>().Use<Bus>());
            return bus;
        }
    }
}
