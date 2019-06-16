using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class MarketStateDto : ISubjectDto
    {
        public String Currency { get; set; }
        public String Subject  { get; set; }
        public double Value { get; set; }

        public static MarketStateDto Random(string asset)
        {
            var rand = new Random();

            return new MarketStateDto()
            {
                Subject = asset,
                Currency = "EUR",
                Value = rand.NextDouble() * 10
            };
        }
    }
}
