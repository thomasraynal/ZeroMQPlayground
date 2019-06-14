using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class MarketConfiguration
    {
        public string Name { get; set; }
        public TimeSpan RouterConnectionTimeout { get; set; }
        public string GetUpdatesEndpoint { get; set; }
        public string StateRequestEndpoint { get; set; }
        public string PushStateUpdateEndpoint { get; set; }
    }
}
