using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.PushPull
{
    public interface IPeerDirectory : IEventHandler<PeerRegisterCommand>, IEventHandler<PeerUpdatedEvent>
    {
        IEnumerable<IPeer> StateOfTheWorld { get; }
    }
}
