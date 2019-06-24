using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Dto;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class StateReply
    {
        public StateReply()
        {
            Events = new List<IEventMessage>();
        }

        public string Subject { get; set; }
        public List<IEventMessage> Events { get; set; }
    }
}
