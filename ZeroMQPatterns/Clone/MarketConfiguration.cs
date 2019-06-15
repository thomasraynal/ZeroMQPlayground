using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class MarketConfiguration
    {
        public string Name { get; set; }
        public TimeSpan RouterConnectionTimeout { get; set; }
        public string GetMarketUpdatesEndpoint { get; set; }
        public string GetMarketStateEndpoint { get; set; }
        public string PushMarketUpdateEndpoint { get; set; }
    }
}
