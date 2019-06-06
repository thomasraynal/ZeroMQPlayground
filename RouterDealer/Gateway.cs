using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.RouterDealer
{
    public class Gateway
    {

        private readonly string _toClientsEndpoint;
        private readonly string _toClustersEndpoint;
        private readonly string _clusterStateEndpoint;

        private RouterSocket _toClusters;
        private RouterSocket _toClients;
        private RouterSocket _clusterState;

        private readonly ConfiguredTaskAwaitable _routerProc;
        private readonly ConfiguredTaskAwaitable _clusterAssignmentProc;
        private readonly ConfiguredTaskAwaitable _clusterStateProc;

        private ConcurrentDictionary<Guid, ClusterState> _clustersState;
 
        private BlockingCollection<Work> _works;
        private NetMQPoller _workPoller;
        private NetMQPoller _clusterStatePoller;

        public Gateway(string toClientsEndpoint, string toClustersEndpoint, string clusterStateEndpoint)
        {
            _toClientsEndpoint = toClientsEndpoint;
            _toClustersEndpoint = toClustersEndpoint;
            _clusterStateEndpoint = clusterStateEndpoint;

            _clustersState = new ConcurrentDictionary<Guid, ClusterState>();

            _works = new BlockingCollection<Work>();

            _routerProc = Task.Run(Start).ConfigureAwait(false);
            _clusterAssignmentProc = Task.Run(Work).ConfigureAwait(false);
            _clusterStateProc = Task.Run(ClusterSate).ConfigureAwait(false);
        }

        private void ClusterSate()
        {
            using (_clusterState = new RouterSocket())
            {
                _clusterState.Bind(_clusterStateEndpoint);
             
                _clusterState.ReceiveReady += (s, e) =>
                {
                    var state = e.Socket.ReceiveMultipartMessage()
                                        .GetMessageFromDealer<ClusterState>();

                    _clustersState.AddOrUpdate(new Guid(state.SenderId), state.Message,(key, value) =>
                     {
                         return value;
                     });
                };

                using (_clusterStatePoller = new NetMQPoller { _clusterState })
                {
                    _clusterStatePoller.Run();
                }
            }

        }

        private Guid NextWorker()
        {
            while (_clustersState.Count == 0)
            {
                Task.Delay(10).Wait();
            }

            return _clustersState.OrderByDescending(state => state.Value.AvailableWorkers)
                                 .FirstOrDefault().Key;
        }

        private void Work()
        {
            foreach (var work in _works.GetConsumingEnumerable())
            {
                work.WorkerId = NextWorker();

                _toClusters.SendMoreFrame(work.WorkerId.ToByteArray())
                           .SendMoreFrameEmpty()
                           .SendFrame(work.Serialize());
            }

        }

        public void Kill()
        {
            _workPoller.Stop();
            _clusterStatePoller.Stop();
        }

        public void Start()
        {
            using (_toClients = new RouterSocket())
            {
                _toClients.Bind(_toClientsEndpoint);

                using (_toClusters = new RouterSocket())
                {
                    _toClusters.Bind(_toClustersEndpoint);

                    using (_workPoller = new NetMQPoller { _toClients, _toClusters })
                    {

                        _toClients.ReceiveReady += (s, e) =>
                        {
                            var transportMessage = e.Socket.ReceiveMultipartMessage()
                                                           .GetMessageFromRouter<Work>();

                            var work = transportMessage.Message;

                            work.ClientId = new Guid(transportMessage.SenderId);

                            if (work.Status == WorkerStatus.Ask)
                            {
                                _works.Add(work);
                            }

                        };

                        _toClusters.ReceiveReady += (s, e) =>
                        {
                            var transportMessage = e.Socket.ReceiveMultipartMessage()
                                                 .GetMessageFromDealer<Work>();

                            var work = transportMessage.Message;

                            if (work.Status == WorkerStatus.Ready)
                            {
                            }
                            else if (work.Status == WorkerStatus.Finished)
                            {
                                _toClients.SendMoreFrame(work.ClientId.ToByteArray())
                                          .SendMoreFrameEmpty()
                                          .SendFrame(transportMessage.MessageBytes);

                            }

                        };

                        _workPoller.Run();

                    }
                }
            }

        }
    }
}
