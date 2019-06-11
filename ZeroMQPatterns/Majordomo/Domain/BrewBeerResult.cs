using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Domain
{
    public class BrewBeerResult: ICommandResult
    {
        public BeerType Type { get; set; }
        public double Quantity { get; set; }
        public Guid WorkerId { get; set; }
    }
}
