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

        public Directory(IBus bus)
        {
            _peers = new Dictionary<Guid, IPeer>() { { bus.Self.Id, bus.Self } };
        }

        public IEnumerable<IPeer> GetMatchedPeers(IEvent @event)
        {
            return _peers.Values.Where(peer => peer.Subscriptions.Any(sub => sub.CanHandle(@event)));
        }

        public void Handle(PeerUpdatedEvent @event)
        {
            _peers[@event.Peer.Id] = @event.Peer;
        }
    }
}
