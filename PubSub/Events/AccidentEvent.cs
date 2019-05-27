using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub.Events
{

    [Routable]
    public class AccidentEvent
    {
        [RoutingPosition(0)]
        public String Datacenter { get; set; }
        [RoutingPosition(1)]
        public String Perimeter { get; set; }
        [RoutingPosition(2)]
        public String Severity { get; set; }

        public override string ToString()
        {
            return $"{Datacenter}.{Perimeter}.{Severity}";
        }
    }
}
