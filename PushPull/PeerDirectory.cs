using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ZeroMQPlayground.PushPull
{
    public class PeerDirectory : IPeerDirectory
    {
        public Dictionary<Guid, IPeer> _peers;
        private readonly IBus _bus;

        public IEnumerable<IPeer> StateOfTheWorld => _peers.Values;

        public PeerDirectory(IBus bus)
        {
            _bus = bus;
            _peers = new Dictionary<Guid, IPeer>();
        }

        public void Handle(PeerRegisterCommand @event)
        {
            _peers[@event.Peer.Id] = @event.Peer;

            _bus.Send(new PeerRegisterCommandResult()
            {
                CommandId = @event.CommandId,
                StateOfTheWorld = _peers.Values.ToList()

            }, @event.Peer);
        }

        public void Handle(PeerUpdatedEvent @event)
        {
            _peers[@event.Peer.Id] = @event.Peer;
        }
    }
}
