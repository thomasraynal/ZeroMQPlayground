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
            Events = new List<byte[]>();
        }

        public string Topic { get; set; }
        public List<byte[]> Events { get; set; }
    }
}
