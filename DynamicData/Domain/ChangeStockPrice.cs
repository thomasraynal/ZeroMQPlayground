using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class ChangeStockPrice : CommandBase<Stock>
    {
        public double Ask { get; set; }
        public double Bid { get; set; }
        public double Mid { get; set; }
        public double Spread { get; set; }

        public override void Apply(Stock aggregate)
        {
            aggregate.Ask = Ask;
            aggregate.Bid = Bid;
            aggregate.Mid = Mid;
            aggregate.Spread = Spread;
        }
    }
}
