using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.Shared
{
    public interface IPeerDirectory : IEventHandler<PeerRegisterCommand>, IEventHandler<PeerUpdatedEvent>
    {
        IEnumerable<IPeer> StateOfTheWorld { get; }
    }
}
