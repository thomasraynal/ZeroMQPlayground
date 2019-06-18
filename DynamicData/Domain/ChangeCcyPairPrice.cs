using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class ChangeCcyPairPrice : CommandBase<string, CurrencyPair>
    {
        public ChangeCcyPairPrice(string stockId, double ask, double bid, double mid, double spread): base(stockId)
        {
            Ask = ask;
            Bid = bid;
            Mid = mid;
            Spread = spread;
        }
        public double Ask { get; set; }
        public double Bid { get; set; }
        public double Mid { get; set; }
        public double Spread { get; set; }

        public override void Apply(CurrencyPair aggregate)
        {
            aggregate.Ask = Ask;
            aggregate.Bid = Bid;
            aggregate.Mid = Mid;
            aggregate.Spread = Spread;
        }
    }
}
