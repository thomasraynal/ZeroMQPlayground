using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.ParanoidPirate
{
    [TestFixture]
    public class TestParanoidPirate
    {
        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        public async Task TestE2E()
        {
            var gatewayToClientsEndpoint = "tcp://localhost:8080";
            var gatewayToWorkersEndpoint = "tcp://localhost:8181";
            var gatewayHeartbeatEndpoint = "tcp://localhost:8282";
            var cancelationTokenSource = new CancellationTokenSource();

            var gateway = new Gateway(gatewayToClientsEndpoint, gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint, cancelationTokenSource.Token);

            await Task.Delay(500);

            var clients = Enumerable.Range(0, 2)
                                    .Select(index => new Client(gatewayToClientsEndpoint, gatewayHeartbeatEndpoint, cancelationTokenSource.Token))
                                    .ToList();


            var workers = Enumerable.Range(0, 3)
                                    .Select(index => new Worker(gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint, cancelationTokenSource.Token))
                                    .ToList();

            var gatewayHeartbeats = clients.Select(client => client.IsConnected).Concat(workers.Select(worker => worker.IsConnected));

            var isGatewayUp = false;

            var isGatewayUpObservable = Observable.Merge(gatewayHeartbeats)
                             .Subscribe(heartbeat =>
                             {
                                 isGatewayUp = heartbeat;
                             });

            await Task.Delay(500);

            Assert.IsTrue(isGatewayUp);

            var works = clients.SelectMany(client => Enumerable.Range(0,5).Select(_=> client.DoWork())).ToList();

            var results = await Task.WhenAll(works);

            Assert.AreEqual(workers.Count(), results.Select(result => result.WorkerId).Distinct().Count());

            Assert.IsTrue(isGatewayUp);

            gateway.Kill();

            await Task.Delay(1000);

            Assert.IsFalse(isGatewayUp);

            gateway = new Gateway(gatewayToClientsEndpoint, gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint, cancelationTokenSource.Token);

            //be sure all heartbeats are run...
            await Task.Delay(1500);

            Assert.IsTrue(isGatewayUp);

            works = clients.Select(client =>  client.DoWork()).ToList();

            Assert.DoesNotThrowAsync(async () =>
            {
                await Task.WhenAll(works);
            });

            cancelationTokenSource.Cancel();

            gateway.Kill();

            workers.ForEach(worker => worker.Stop());

            

        }
    }
}
