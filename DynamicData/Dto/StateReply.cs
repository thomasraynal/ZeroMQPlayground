using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class StateReply<TKey, TAggregate> where TAggregate : IAggregate<TKey>
    {
        public string Topic { get; set; }

        public List<IEvent<TKey, TAggregate>> Events { get; set; }
    }

    public class StateReply
    {
        public StateReply()
        {
            Events = new List<TransportMessage>();
        }

        public string Topic { get; set; }
        public List<TransportMessage> Events { get; set; }
    }
}
