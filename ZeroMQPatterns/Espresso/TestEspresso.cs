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

namespace ZeroMQPlayground.ZeroMQPatterns.Expresso
{

    [TestFixture]
    public class TestEspresso
    {
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        public async Task TestE2E()
        {
            var publisherEnpoint = "tcp://localhost:8080";
            var routerBackend = "tcp://localhost:8181";
            var cancelationTokenSource = new CancellationTokenSource();

            var broker = new Broker(publisherEnpoint, routerBackend, cancelationTokenSource.Token);

            await Task.Delay(500);

            var market = new FxMarket("FxMarket", publisherEnpoint, cancelationTokenSource.Token);

            await Task.Delay(500);

            var trader1 = new TraderDesktop(routerBackend, cancelationTokenSource.Token);

            await Task.Delay(500);

            var price1 = trader1.GetLastPrice(FxMarket.CCyPairWithUniquePrice);

            Assert.IsNotNull(price1);

            var trader2 = new TraderDesktop(routerBackend, cancelationTokenSource.Token);

            await Task.Delay(1000);

            var price2 = trader2.GetLastPrice(FxMarket.CCyPairWithUniquePrice);

            Assert.IsNotNull(price2);

            var trader3 = new TraderDesktop(routerBackend, cancelationTokenSource.Token);

            await Task.Delay(1000);

            var price3 = trader3.GetLastPrice(FxMarket.CCyPairWithUniquePrice);

            Assert.IsNotNull(price3);

            cancelationTokenSource.Cancel();
            broker.Kill();

            Assert.AreEqual(price1.Mid, price2.Mid);
            Assert.AreEqual(price2.Mid, price3.Mid);

        }
    }
}
