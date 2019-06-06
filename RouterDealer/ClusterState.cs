using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.RouterDealer
{
    public class ClusterState
    {
        public int AvailableWorkers { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
