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

        public static MarketStateDto Random(string asset)
        {
            var rand = new Random();

            return new MarketStateDto()
            {
                Asset = asset,
                Currency = "EUR",
                Value = rand.NextDouble() * 10
            };
        }

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
