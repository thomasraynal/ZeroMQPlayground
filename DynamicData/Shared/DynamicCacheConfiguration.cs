using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class DynamicCacheConfiguration
    {
        public string SubscriptionEndpoint { get; set; }
        public string StateoftheWorldEndpoint { get; set; }
        public string HearbeatEndpoint { get; set; }
    }
}
