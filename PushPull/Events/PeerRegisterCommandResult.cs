using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PushPull
{
    public class PeerRegisterCommandResult : ICommandResult
    {
        public List<IPeer> StateOfTheWorld { get; set; }
        public Guid CommandId { get; set; }
    }
}
