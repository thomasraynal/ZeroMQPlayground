﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class ClientConfiguration
    {
        public string Name { get; set; }
        public TimeSpan RouterConnectionTimeout { get; set; }
        public string SubscribeToUpdatesEndpoint { get; set; }
        public string GetStateEndpoint { get; set; }
        public string PublishUpdateEndpoint { get; set; }
        public string BrokerHeartbeatEndpoint { get; set; }
    }
}
