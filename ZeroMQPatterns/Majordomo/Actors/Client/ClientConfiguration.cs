using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class ClientConfiguration
    {
        public ClientConfiguration(TimeSpan commandTimeout, string gatewayEndpoint, string gatewayHeartbeatEndpoint, TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay)
        {
            CommandTimeout = commandTimeout;
            GatewayEndpoint = gatewayEndpoint;
            GatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            HearbeatDelay = hearbeatDelay;
            HearbeatMaxDelay = hearbeatMaxDelay;
        }

        public TimeSpan CommandTimeout { get; }
        public String GatewayEndpoint { get; }
        public String GatewayHeartbeatEndpoint { get; }
        public TimeSpan HearbeatDelay { get; }
        public TimeSpan HearbeatMaxDelay { get; }
    }
}
