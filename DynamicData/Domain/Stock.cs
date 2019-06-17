using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class Stock : AggregateBase<string>
    {

        public StockState State { get; set; }

        public string Currency { get; set; }
        public double Ask { get; set; }
        public double Bid { get; set; }
        public double Mid { get; set; }
        public double Spread { get; set; }
    }
}
