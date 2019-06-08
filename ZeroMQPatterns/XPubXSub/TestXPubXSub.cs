using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.XPubXSub
{

    [TestFixture]
    public class TestXPubXSub
    {
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        public async Task TestE2E()
        {
            var brokerFrontend = "tcp://localhost:8080";
            var brokerBackend = "tcp://localhost:8181";
            var cancelationTokenSource = new CancellationTokenSource();

            var broker = new Broker(brokerFrontend, brokerBackend, cancelationTokenSource.Token);

            await Task.Delay(250);

            var markets = Enumerable.Range(0, 3)
                                    .Select(index => new FxMarket($"FxMarket{index}", brokerFrontend, cancelationTokenSource.Token))
                                    .ToList();

            var traders = Enumerable.Range(0, 2)
                                    .Select(index => new TraderDesktop(brokerBackend, cancelationTokenSource.Token))
                                    .ToList();

            await Task.Delay(2000);

            cancelationTokenSource.Cancel();
            broker.Kill();

            var price1 = traders[0].GetLastPrice("EUR/USD");
            var price2 = traders[1].GetLastPrice("EUR/USD");

            Assert.AreEqual(price1.Mid, price2.Mid);

        }
    }
}
