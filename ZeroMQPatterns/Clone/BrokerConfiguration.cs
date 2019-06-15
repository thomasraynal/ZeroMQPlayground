using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class BrokerConfiguration
    {
        public string GetMarketUpdatesEndpoint { get; set; }
        public string GetMarketStateEndpoint { get; set; }
        public string PushMarketUpdateEndpoint { get; set; }
    }
}
