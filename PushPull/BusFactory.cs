using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public static class BusFactory
    {
        public static IBus Create<TRegistry>(IBusConfiguration configuration) where TRegistry : Registry
        {
            var container = new Container((conf) => conf.AddRegistry(Activator.CreateInstance<TRegistry>()));

            container.Configure(conf => conf.For<IBusConfiguration>().Use(configuration));

            var self = new Peer(Guid.NewGuid(), configuration.PeerName, configuration.Endpoint);
            container.Configure(conf => conf.For<IPeer>().Use(self));

            var peerDirectoryDescriptor = new Peer(Guid.NewGuid(), "PeerDirectory", configuration.DirectoryEndpoint);

            var bus = new Bus(container, self, peerDirectoryDescriptor);
            container.Configure(conf => conf.For<IBus>().Use(bus));

            if (configuration.IsPeerDirectory)
            {
                var peerDirectory = new PeerDirectory(bus);
                container.Configure(conf => conf.For<IPeerDirectory>().Use(peerDirectory));
                bus.Register<PeerRegisterCommand>(peerDirectory);
                bus.Register<PeerUpdatedEvent>(peerDirectory);
            }

            bus.Start();

            return bus;
        }
    }
}
