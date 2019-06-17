using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Domain;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    [TestFixture]
    public class TestDynamicData
    {
        private readonly Random _rand = new Random();
        private static readonly string[] CcyPairs = { "EUR/USD", "EUR/JPY", "EUR/GBP" };
        private Stock Next()
        {
            var mid = _rand.NextDouble() * 10;
            var spread = _rand.NextDouble() * 2;

            var price = new Stock()
            {
                Ask = mid + spread,
                Bid = mid - spread,
                Mid = mid,
                Spread = spread,
                State = StockState.Open,
                Id =  CcyPairs[_rand.Next(0, 3)],
                Currency = "EUR"
            };

            return price;
        }

        [Test]
        public void TestEventApply()
        {
            var stock = Next();

            var changeStateClose = new ChangeStockState()
            {
                State = StockState.Close
            };

            stock.Apply(changeStateClose);

            Assert.AreEqual(StockState.Close, stock.State);

            var changeStateOpen = new ChangeStockState()
            {
                State = StockState.Open
            };

            stock.Apply(changeStateOpen as IEvent);

            Assert.AreEqual(StockState.Open, stock.State);

        }

    }
}
