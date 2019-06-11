using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Domain
{
    public class TeaMaker : Worker<MakeTea, MakeTeaResult>
    {
        private readonly Random _rand;

        public TeaMaker(string gatewayEndpoint, string gatewayHeartbeatEndpoint) : base(gatewayEndpoint, gatewayHeartbeatEndpoint)
        {
            _rand = new Random();
        }

        public async override Task<MakeTeaResult> Handle(MakeTea command)
        {
            var beer = new MakeTeaResult()
            {
                Quantity = Math.Round(_rand.NextDouble() * 10, 2),
                Type = (TeaType)_rand.Next(0, Enum.GetNames(typeof(TeaType)).Count())
            };

            await Task.Delay(_rand.Next(250, 500));

            return beer;
        }
    }
}
