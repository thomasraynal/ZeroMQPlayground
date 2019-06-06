using NetMQ;
using NetMQ.Sockets;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.RouterDealer
{

    [TestFixture]
    public class TestRouterDealer
    {
        class ClusterPack
        {
            public Cluster Cluster { get; set; }
            public List<Worker> Workers { get; set; }
        }


        private async Task<ClusterPack> CreateCluster(string clusterEndpoint, string toGatewayEndpoint, string gatewayClusterStateEndpoint, int nbWorkers, CancellationToken token)
        {
            var cluster = new Cluster(toGatewayEndpoint, gatewayClusterStateEndpoint, clusterEndpoint);

            await Task.Delay(250);

            var result =  new ClusterPack()
            {
                Cluster = cluster,
                Workers = Enumerable.Range(0, nbWorkers).Select(_ => new Worker(clusterEndpoint, token)).ToList()
            };

            await Task.Delay(250);

            return result;
        }

        [Test]
        public async Task TestE2E()
        {
            var gatewayFrontend = "tcp://localhost:8080";
            var gatewayBackend = "tcp://localhost:8181";
            var gatewayClusterStateEnpoint = "tcp://localhost:8282";

            var cluster1Endpoint = "tcp://localhost:8383";
            var cluster2Endpoint = "tcp://localhost:8484";

            var cancellation = new CancellationTokenSource();

            var client = new Client(gatewayFrontend);
            var gateway = new Gateway(gatewayFrontend, gatewayBackend, gatewayClusterStateEnpoint);

            await Task.Delay(250);

            var cluster1 = await CreateCluster(cluster1Endpoint, gatewayBackend, gatewayClusterStateEnpoint, 2, cancellation.Token);
            var cluster2 = await CreateCluster(cluster2Endpoint, gatewayBackend, gatewayClusterStateEnpoint, 2, cancellation.Token);

            var tasks = Enumerable.Range(0, 10).Select(_ => client.DoWork());

            var results = await Task.WhenAll(tasks);

            Assert.AreEqual(10, results.Count());

            var availableWorkers = new[] { cluster1, cluster2 }.SelectMany(pack => pack.Workers).Select(worker => worker.WorkerId);

            var usedWorkers = results.Select(work => work.WorkerId).Distinct();

            var areAllWOrkersUsed = usedWorkers.All(w => availableWorkers.Any(w1 => w1 == w));

            Assert.IsTrue(areAllWOrkersUsed);

            cancellation.Cancel();

        }

    }
}


