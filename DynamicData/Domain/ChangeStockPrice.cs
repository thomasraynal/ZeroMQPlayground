using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class ChangeStockPrice : CommandBase<Stock>
    {
        public ChangeStockPrice(string stockId, double ask, double bid, double mid, double spread)
        {
            StockId = stockId;
            Ask = ask;
            Bid = bid;
            Mid = mid;
            Spread = spread;
        }
        public string StockId { get; set; }
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
