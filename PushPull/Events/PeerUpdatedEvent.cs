using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class PeerUpdatedEvent : IEvent
    {
        public IPeer Peer { get; set; }
    }
}
