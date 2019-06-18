using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class DynamicCacheConfiguration
    {
        public DynamicCacheConfiguration(string subscriptionEndpoint, string stateOfTheWorldEndpoint, string hearbeatEndpoint)
        {
            SubscriptionEndpoint = subscriptionEndpoint;
            HearbeatEndpoint = hearbeatEndpoint;
            StateOfTheWorldEndpoint = stateOfTheWorldEndpoint;

            IsStaleTimespan = TimeSpan.MaxValue;
            ZmqHighWatermark = 1000;
        }

        public int ZmqHighWatermark { get; set; }
        public TimeSpan IsStaleTimespan { get; set; }
        public string StateOfTheWorldEndpoint { get; }
        public string SubscriptionEndpoint { get; }
        public string HearbeatEndpoint { get; }
    }
}
