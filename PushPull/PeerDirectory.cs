using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class PeerDirectory : IPeerDirectory
    {
        public Dictionary<Guid, IPeer> _peers;
        private readonly IBus _bus;

        public PeerDirectory(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(PeerRegisterCommand @event)
        {
            _peers[@event.Peer.Id] = @event.Peer;

            _bus.Emit(new PeerRegisterCommandResult()
            {
                StateOfTheWorld = _peers.Values.ToList()
            });
        }

        public void Handle(PeerUpdatedEvent @event)
        {
            _peers[@event.Peer.Id] = @event.Peer;
        }
    }
}
