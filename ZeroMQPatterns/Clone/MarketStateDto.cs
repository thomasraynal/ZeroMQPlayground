using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class MarketStateDto
    {
        public String Currency { get; set; }
        public String Asset { get; set; }
        public double Value { get; set; }

        public MarketStateUpdate ToMarketStateUpdate(long eventId)
        {
            return new MarketStateUpdate()
            {
                Asset = Asset,
                Currency = Currency,
                Value = Value,
                EventSequentialId = eventId
            };
        }
    }
}
