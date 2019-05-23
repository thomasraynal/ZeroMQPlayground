using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class PeerRegisterCommand : ICommand
    {
        public IPeer Peer { get; set; }
    }
}
