using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.Shared
{
    public interface IDirectory : IEventHandler<PeerRegisteredEvent>,
                                 IEventHandler<PeerRegisterCommand>
    {
        IEnumerable<IPeer> GetMatchedPeers(IEvent @event);
    }
}
