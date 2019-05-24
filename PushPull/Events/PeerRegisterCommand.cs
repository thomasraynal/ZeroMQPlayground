using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class PeerRegisterCommand : ICommand
    {
        public PeerRegisterCommand()
        {
            CommandId = Guid.NewGuid();
        }

        public IPeer Peer { get; set; }
        public Guid CommandId { get; set; } 
    }
}
