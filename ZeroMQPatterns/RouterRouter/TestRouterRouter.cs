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

namespace ZeroMQPlayground.ZeroMQPatterns.RouterRouter
{
    [TestFixture]
    public class TestRouterRouter
    {
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }


        [Test]
        public async Task TestE2E()
        {
            var loadbalancerFrontend = "tcp://localhost:8080";
            var loadbalancerBackend = "tcp://localhost:8181";
            var cancellation = new CancellationTokenSource();

            var router = new LoadBalancer(loadbalancerFrontend, loadbalancerBackend);
            await Task.Delay(500);

            var worker1 = new Worker(loadbalancerBackend, cancellation.Token);
            var worker2 = new Worker(loadbalancerBackend, cancellation.Token);
            var worker3 = new Worker(loadbalancerBackend, cancellation.Token);
            var worker4 = new Worker(loadbalancerBackend, cancellation.Token);

            var client = new Client(loadbalancerFrontend);

            var tasks = Enumerable.Range(0, 100).Select(_ => client.DoWork());

            var works = await Task.WhenAll(tasks);

            cancellation.Cancel();
            router.Kill();

            var usedWorkers = works.Select(work => Encoding.UTF8.GetString(work.WorkerId)).Distinct();

            Assert.AreEqual(4, usedWorkers.Count());

       
        }

    }
}
