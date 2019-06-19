using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class StateReply
    {
        public StateReply()
        {
            Events = new List<TransportMessage>();
        }

        public string Subject { get; set; }
        public List<TransportMessage> Events { get; set; }
    }
}
