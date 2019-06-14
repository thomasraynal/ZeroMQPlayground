using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class MarketStateUpdate
    {
        public String Currency { get; set; }
        public String Asset { get; set; }
        public double Value { get; set; }

        public long EventSequentialId { get; set; }
    }
}
