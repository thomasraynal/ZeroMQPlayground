using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Domain
{
    public class BeerBrewer : Worker<BrewBeer, BrewBeerResult>
    {
        private readonly Random _rand;

        public BeerBrewer(string gatewayEndpoint, string gatewayHeartbeatEndpoint) : base(gatewayEndpoint, gatewayHeartbeatEndpoint)
        {
            _rand = new Random();
        }

        public async override Task<BrewBeerResult> Handle(BrewBeer command)
        {
            var beer = new BrewBeerResult()
            {
                Quantity = Math.Round(_rand.NextDouble() * 10, 2),
                Type = (BeerType)_rand.Next(0, Enum.GetNames(typeof(BeerType)).Count())
            };

            await Task.Delay(_rand.Next(250, 500));

            return beer;
        }
    }
}
