using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class BrokerConfiguration
    {
        public string PublishUpdatesEndpoint { get; set; }
        public string SendStateEndpoint { get; set; }
        public string SubscribeToUpdatesEndpoint { get; set; }
        public string HeartbeatEndpoint { get; set; }
        public IContainer Container { get; set; }
    }
}
