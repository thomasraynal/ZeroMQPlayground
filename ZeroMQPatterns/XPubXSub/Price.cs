using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.XPubXSub
{
    public class Price
    {
        public string Asset { get; set; }
        public string Currency { get; set; }
        public double Ask { get; set; }
        public double Bid { get; set; }

        public double Mid { get; set; }

        public double Spread { get; set; }
    }
}
