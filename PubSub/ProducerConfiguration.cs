using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class ProducerConfiguration
    {
        public bool IsTest { get; set; }
        public Guid Id { get; set; }
        public String Endpoint { get; set; }
        public String EndpointForClient { get; set; }

        public String HeartbeatEnpoint { get; set; }
    }
}
