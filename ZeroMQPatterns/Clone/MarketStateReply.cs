using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class MarketStateReply
    {
        public List<MarketStateUpdate> Updates { get; set; }
    }
}
