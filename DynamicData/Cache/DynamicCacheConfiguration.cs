﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class DynamicCacheConfiguration : IDynamicCacheConfiguration
    {
        public DynamicCacheConfiguration(string subscriptionEndpoint, string stateOfTheWorldEndpoint, string hearbeatEndpoint)
        {
            SubscriptionEndpoint = subscriptionEndpoint;
            HearbeatEndpoint = hearbeatEndpoint;
            StateOfTheWorldEndpoint = stateOfTheWorldEndpoint;

            StateCatchupTimeout = TimeSpan.FromSeconds(10);
            HeartbeatDelay = TimeSpan.FromSeconds(10);
            HeartbeatTimeout = TimeSpan.FromSeconds(1);

            IsStaleTimeout = TimeSpan.MaxValue;
            ZmqHighWatermark = 1000;

            Subject = string.Empty;
        }

        public string Subject { get; set; }
        public int ZmqHighWatermark { get; set; }
        public TimeSpan HeartbeatDelay { get; set; }
        public TimeSpan HeartbeatTimeout { get; set; }
        public TimeSpan StateCatchupTimeout { get; set; }
        public TimeSpan IsStaleTimeout { get; set; }
        public string StateOfTheWorldEndpoint { get; set; }
        public string SubscriptionEndpoint { get; set; }
        public string HearbeatEndpoint { get; set; }
    }
}
