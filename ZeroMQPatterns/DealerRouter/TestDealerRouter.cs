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

namespace ZeroMQPlayground.ZeroMQPatterns.DealerRouter
{
    [TestFixture]
    public class TestDealerRouter
    {
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }


        [Test]
        public async Task TestE2E()
        {
            var routerEndpoint = "tcp://localhost:8080";
            var dealerEndpoint = "tcp://localhost:8181";
            var cancellation = new CancellationTokenSource();

            var consumer1 = new Consumer(dealerEndpoint, cancellation.Token);
            var consumer2 = new Consumer(dealerEndpoint, cancellation.Token);
            var consumer3 = new Consumer(dealerEndpoint, cancellation.Token);
            var consumer4 = new Consumer(dealerEndpoint, cancellation.Token);

            var producer1 = new Producer(routerEndpoint, cancellation.Token);
            var producer2 = new Producer(routerEndpoint, cancellation.Token);

            var router = new Router(routerEndpoint, dealerEndpoint, cancellation.Token);


            await Task.Delay(1000);


            Assert.Greater(consumer1.Received.Count, 0);
            Assert.Greater(consumer2.Received.Count, 0);
            Assert.Greater(consumer3.Received.Count, 0);
            Assert.Greater(consumer4.Received.Count, 0);


            cancellation.Cancel();

        }
    }
}
