using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class PeerRegisteredEvent : IEvent
    {
        public IPeer AcknowledgePeer { get; set; }
    }
}
