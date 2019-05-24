using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.Shared
{
    public interface IDirectory : IEventHandler<PeerUpdatedEvent>, IEventHandler<PeerRegisterCommandResult>
    {
        IEnumerable<IPeer> GetMatchedPeers(IEvent @event);
        Task Start();
    }
}
