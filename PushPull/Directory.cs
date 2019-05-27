using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ZeroMQPlayground.PushPull
{
    public class Directory : IDirectory
    {
        public Dictionary<Guid,IPeer> _peers;
        private IBus _bus;

        public Directory(IBus bus)
        {
            _peers = new Dictionary<Guid, IPeer>() { { bus.Self.Id, bus.Self } };
            _bus = bus;

            _bus.Register<PeerUpdatedEvent>(this);
            _bus.Register<PeerRegisterCommandResult>(this);
        }

        public IEnumerable<IPeer> GetMatchedPeers(IEvent @event)
        {
            return _peers.Values.Where(peer => peer.Subscriptions.Any(sub => sub.CanHandle(@event)));
        }

        public void Handle(PeerUpdatedEvent @event)
        {
            _peers[@event.Peer.Id] = @event.Peer;
        }

        public void Handle(PeerRegisterCommandResult @event)
        {
        }

        public async Task Start()
        {
            var registrationResult = await _bus.Send<PeerRegisterCommandResult>(new PeerRegisterCommand()
            {
                Peer = _bus.Self

            }, _bus.PeerDirectory);

            foreach(var peer in registrationResult.StateOfTheWorld)
            {
                _peers[peer.Id] = peer;
            }

        }
    }
}
