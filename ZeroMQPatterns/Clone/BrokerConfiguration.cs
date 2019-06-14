using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class BrokerConfiguration
    {
        public string GetUpdatesEndpoint { get; set; }
        public string StateRequestEndpoint { get; set; }
        public string PushStateUpdateEndpoint { get; set; }
    }
}
