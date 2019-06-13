using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class WorkerConfiguration
    {
        public WorkerConfiguration(string gatewayEndpoint, string gatewayHeartbeatEndpoint, TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay)
        {
            GatewayEndpoint = gatewayEndpoint;
            GatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            HearbeatDelay = hearbeatDelay;
            HearbeatMaxDelay = hearbeatMaxDelay;
        }

        public String GatewayEndpoint { get; }
        public String GatewayHeartbeatEndpoint { get; }
        public TimeSpan HearbeatDelay { get; }
        public TimeSpan HearbeatMaxDelay { get; }
    }
}
