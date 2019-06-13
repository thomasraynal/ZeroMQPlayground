using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class GatewayConfiguration
    {
        public GatewayConfiguration(string toClientsEndpoint, string toWorkersEndpoint, string heartbeatEndpoint, TimeSpan workerTtl, TimeSpan workTtl)
        {
            ToClientsEndpoint = toClientsEndpoint;
            ToWorkersEndpoint = toWorkersEndpoint;
            HeartbeatEndpoint = heartbeatEndpoint;
            WorkerTtl = workerTtl;
            WorkTtl = workTtl;
        }

        public String ToClientsEndpoint { get; }
        public String ToWorkersEndpoint { get; }
        public String HeartbeatEndpoint { get; }
        public TimeSpan WorkerTtl { get; }
        public TimeSpan WorkTtl { get; }
    }
}
