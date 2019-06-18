using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class CurrencyPair : AggregateBase<string>
    {

        public CcyPairState State { get; set; }

        public double Ask { get; set; }
        public double Bid { get; set; }
        public double Mid { get; set; }
        public double Spread { get; set; }
    }
}
