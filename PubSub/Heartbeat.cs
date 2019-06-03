using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class HeartbeatQuery
    {
        public string HeartbeatEndpoint { get; set; }
    }

    public class HeartbeatResponse
    {
        public Guid ProducerId { get; set; }
        public DateTime Now { get; set; }
    }
}
