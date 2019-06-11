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
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Domain;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    [TestFixture]
    public class TestMajordomo
    {
        [Test]
        public async Task TestE2E()
        {
            var gatewayToClientsEndpoint = "tcp://localhost:8080";
            var gatewayToWorkersEndpoint = "tcp://localhost:8181";
            var gatewayHeartbeatEndpoint = "tcp://localhost:8282";

            var gateway = new Gateway(gatewayToClientsEndpoint, gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint);

            await gateway.Start();

            await Task.Delay(500);

            var clients = Enumerable.Range(0, 2)
                                    .Select(_ =>
                                    {
                                        var client = new Client(gatewayToClientsEndpoint, gatewayHeartbeatEndpoint);
                                        client.Start().Wait();
                                        return client;
                                    })
                                    .ToList();


            var workers = Enumerable.Range(0, 2)
                                    .Select(_ =>
                                    {
                                        IWorker makeTea = new TeaMaker(gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint);
                                        IWorker brewBeer = new BeerBrewer(gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint);
                                        makeTea.Start().Wait();
                                        brewBeer.Start().Wait();
                                        return new IWorker[] { makeTea, brewBeer };
                                    })
                                    .SelectMany(w => w)
                                    .ToList();

            var gatewayHeartbeats = clients.Select(client => client.IsConnected)
                                           .Concat(workers.Select(worker => worker.IsConnected));

            var isGatewayUp = false;

            var isGatewayUpObservable = Observable.Merge(gatewayHeartbeats)
                             .Subscribe(heartbeat =>
                             {
                                 isGatewayUp = heartbeat;
                             });

            //ensure all heartbeats are fired...
            await Task.Delay(1500);

            Assert.IsTrue(isGatewayUp);

            var works = clients.SelectMany(client => Enumerable.Range(0, 5)
                               .Select(_ => client.Send<MakeTea,MakeTeaResult>(new MakeTea()))).ToList();

            var results = await Task.WhenAll(works);

            Assert.AreEqual(workers.Count(), results.Select(result => result.WorkerId).Distinct().Count());

            Assert.IsTrue(isGatewayUp);

            await gateway.Stop();

            await Task.Delay(1000);

            Assert.IsFalse(isGatewayUp);

            gateway = new Gateway(gatewayToClientsEndpoint, gatewayToWorkersEndpoint, gatewayHeartbeatEndpoint);

            //be sure all heartbeats are run...
            await Task.Delay(1500);

            Assert.IsTrue(isGatewayUp);

            works = clients.SelectMany(client => Enumerable.Range(0, 5)
                           .Select(_ => client.Send<MakeTea, MakeTeaResult>(new MakeTea()))).ToList();

            Assert.DoesNotThrowAsync(async () =>
            {
                await Task.WhenAll(works);
            });

            var stop = new[] { gateway.Stop() }.Concat(workers.Select(worker => worker.Stop())).Concat(clients.Select(client => client.Stop()));

            await Task.WhenAll(stop);

        }
    }
}
