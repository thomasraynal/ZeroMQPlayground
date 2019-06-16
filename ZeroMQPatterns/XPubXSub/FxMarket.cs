using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.XPubXSub
{
    public class FxMarket : MarketPricePublisherBase
    {
        public static readonly string[] CcyPairs = { "EUR/USD", "EUR/JPY", "EUR/GBP" };
        private readonly Random _rand;

        public FxMarket(string name,  string brokerEndpoint, CancellationToken token) : base(name, "FX", brokerEndpoint, token)
        {
            _rand = new Random();
        }

        public override Price Next()
        {
            var mid = _rand.NextDouble() * 10;
            var spread = _rand.NextDouble() * 2;

            return new Price()
            {
                Ask = mid + spread,
                Bid = mid - spread,
                Mid = mid,
                Spread = spread,
                Asset = CcyPairs[_rand.Next(0, 3)],
                Currency = "EUR"
            };
        }

    }
}
