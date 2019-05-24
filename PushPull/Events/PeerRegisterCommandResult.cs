using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class PeerRegisterCommandResult : ICommandResult
    {
        public List<IPeer> StateOfTheWorld { get; set; }
        public Guid CommandId { get; set; }
    }
}
