using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Directory : IDirectory
    {
        public Dictionary<Guid,IPeer> _peers;
        private readonly IBus _bus;

        public Directory(IBus bus)
        {
            _peers = new Dictionary<Guid, IPeer>() { { bus.Self.Id, bus.Self } };
            _bus = bus;
        }

        public IEnumerable<IPeer> GetMatchedPeers(IEvent @event)
        {
            return _peers.Values.Where(peer => peer.Subscriptions.Any(sub => sub.CanHandle(@event)));
        }

        public void Handle(PeerRegisteredEvent @event)
        {
            _peers.Add(@event.AcknowledgePeer.Id, @event.AcknowledgePeer);
        }

        public void Handle(PeerRegisterEvent @event)
        {
            _bus.Emit(new PeerRegisteredEvent()
            {
                AcknowledgePeer = _bus.Self
            });
        }
    }
}
