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

        private const string defaultGatewayToClientsEndpoint = "tcp://localhost:8080";
        private const string defaultGatewayToWorkersEndpoint = "tcp://localhost:8181";
        private const string defaultGatewayHeartbeatEndpoint = "tcp://localhost:8282";


        private static readonly GatewayConfiguration DefaultGatewayConfiguration = new GatewayConfiguration(
                                                            defaultGatewayToClientsEndpoint,
                                                            defaultGatewayToWorkersEndpoint,
                                                            defaultGatewayHeartbeatEndpoint,
                                                            workerTtl: TimeSpan.FromMilliseconds(5000),
                                                            workTtl: TimeSpan.FromMilliseconds(5000));

        private static readonly ClientConfiguration DefaultClientConfiguration = new ClientConfiguration(
                                                            commandTimeout: TimeSpan.FromMilliseconds(5000),
                                                            defaultGatewayToClientsEndpoint,
                                                            defaultGatewayHeartbeatEndpoint,
                                                            hearbeatDelay: TimeSpan.FromMilliseconds(250),
                                                            hearbeatMaxDelay: TimeSpan.FromMilliseconds(1000));

        private static readonly WorkerConfiguration DefaultWorkerConfiguration = new WorkerConfiguration(
                                                            defaultGatewayToWorkersEndpoint,
                                                            defaultGatewayHeartbeatEndpoint,
                                                            hearbeatDelay: TimeSpan.FromMilliseconds(250),
                                                            hearbeatMaxDelay: TimeSpan.FromMilliseconds(1000));

        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        public async Task TestWorkerDisconnectAndReconnect()
        {


            var gateway = new Gateway(DefaultGatewayConfiguration);
            await gateway.Start();

            await Task.Delay(500);

            var client = new Client(DefaultClientConfiguration);
            await client.Start();

            await Task.Delay(500);

            var worker = new BeerBrewer(DefaultWorkerConfiguration);
            await worker.Start();

            await Task.Delay(500);

            BrewBeerResult result = null;

            Assert.DoesNotThrowAsync(async () =>
            {
                result = await client.Send<BrewBeer, BrewBeerResult>(new BrewBeer());
            });

            Assert.IsNotNull(result);

            await worker.Stop();

            //ensure all heartbeats are run
            await Task.Delay(2000);

            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await client.Send<BrewBeer, BrewBeerResult>(new BrewBeer());
            });

            worker = new BeerBrewer(DefaultWorkerConfiguration);
            await worker.Start();

            //ensure all heartbeats are run
            await Task.Delay(2000);

            Assert.DoesNotThrowAsync(async () =>
            {
                result = await client.Send<BrewBeer, BrewBeerResult>(new BrewBeer());
            });

            Assert.IsNotNull(result);

            var stop = new[] { gateway.Stop(), client.Stop(), worker.Stop() };

            await Task.WhenAll(stop);

        }
        [Test]
        public async Task TestCommandTimeout()
        {

            var gateway = new Gateway(DefaultGatewayConfiguration);
            await gateway.Start();

            await Task.Delay(500);

            var client = new Client(DefaultClientConfiguration);
            await client.Start();

            await Task.Delay(500);

            Assert.ThrowsAsync<TaskCanceledException>(async() =>
            {
               await client.Send<MakeTea, MakeTeaResult>(new MakeTea());

            });

            var stop = new[] { gateway.Stop(), client.Stop()};

            await Task.WhenAll(stop);
        }

        [Test]
        public async Task TestE2E()
        {

            var gateway = new Gateway(DefaultGatewayConfiguration);

            await gateway.Start();

            await Task.Delay(500);

            //2 clients
            var clients = Enumerable.Range(0, 2)
                                    .Select(_ =>
                                    {
                                        var client = new Client(DefaultClientConfiguration);
                                        client.Start().Wait();
                                        return client;
                                    })
                                    .ToList();


            //2*worker by service
            var workers = Enumerable.Range(0, 2)
                                    .SelectMany(_ =>
                                    {
                                        IWorker makeTea = new TeaMaker(DefaultWorkerConfiguration);
                                        IWorker brewBeer = new BeerBrewer(DefaultWorkerConfiguration);
                                        makeTea.Start().Wait();
                                        brewBeer.Start().Wait();
                                        return new IWorker[] { makeTea, brewBeer };
                                    })
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
            await Task.Delay(2000);

            Assert.IsTrue(isGatewayUp);

            var teaWorks = clients.SelectMany(client => Enumerable.Range(0, 3).Select(async _ =>
                await client.Send<MakeTea, MakeTeaResult>(new MakeTea())
            )).ToList();

            var bearWorks = clients.SelectMany(client => Enumerable.Range(0, 3).Select(async _ =>
                await client.Send<BrewBeer, BrewBeerResult>(new BrewBeer())
            )).ToList();

            var teaResults = await Task.WhenAll(teaWorks);
            var beerResults = await Task.WhenAll(bearWorks);

            Assert.IsTrue(teaResults.All(work => null != work));
            Assert.IsTrue(beerResults.All(work => null != work));

            //all workers should have get at least a job
            Assert.AreEqual(2, teaResults.Select(result => result.WorkerId).Distinct().Count());
            Assert.AreEqual(2, beerResults.Select(result => result.WorkerId).Distinct().Count());

            Assert.IsTrue(isGatewayUp);

            await gateway.Stop();

            //ensure all heartbeats are fired...
            await Task.Delay(2500);

            Assert.IsFalse(isGatewayUp);

            gateway = new Gateway(DefaultGatewayConfiguration);
            await gateway.Start();

            //ensure all heartbeats are fired...
            await Task.Delay(2500);

            Assert.IsTrue(isGatewayUp);

            //all commands should be handled 
            Assert.DoesNotThrowAsync(async () =>
            {

                var works = clients.SelectMany(client => Enumerable.Range(0, 3)
                            .Select(async _ => await client.Send<MakeTea, MakeTeaResult>(new MakeTea()))).ToList();


                var results = await Task.WhenAll(works);

            });

            var stop = new[] { gateway.Stop() }.Concat(workers.Select(worker => worker.Stop())).Concat(clients.Select(client => client.Stop()));

            await Task.WhenAll(stop);

        }
    }
}
