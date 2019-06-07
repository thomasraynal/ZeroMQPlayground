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
    public class Cluster
    {

        private readonly string _gatewayEndpoint;
        private readonly string _clusterEndpoint;
        private readonly string _gatewayClusterStateEnpoint;
        private readonly Guid _id;

        private RouterSocket _toWorkers;
        private DealerSocket _toGateway;
        private DealerSocket _statePublisher;

        private readonly ConfiguredTaskAwaitable _endpointProc;
        private readonly ConfiguredTaskAwaitable _enqueueWorks;
        private ConfiguredTaskAwaitable _statePublishProc;

        private Queue<Guid> _workers;
        private BlockingCollection<Work> _works;

        private NetMQPoller _workPoller;
        private NetMQPoller _statePublisherPoller;

        private AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public Cluster(string gatewayBackendpoint, string gatewayClusterStateEnpoint, string clusterEndpoint,CancellationToken token)
        {
            _gatewayEndpoint = gatewayBackendpoint;
            _gatewayClusterStateEnpoint = gatewayClusterStateEnpoint;
            _clusterEndpoint = clusterEndpoint;
      
            _id = Guid.NewGuid();

            _workers = new Queue<Guid>();
            _works = new BlockingCollection<Work>();

            _endpointProc = Task.Run(Start, token).ConfigureAwait(false);
            _enqueueWorks = Task.Run(DoWork, token).ConfigureAwait(false);

        }

        private void StatePublish()
        {
            using (_statePublisher = new DealerSocket())
            {
                _statePublisher.Options.Identity = _id.ToByteArray();
                _statePublisher.Connect(_gatewayClusterStateEnpoint);

                var timer = new NetMQTimer(TimeSpan.FromSeconds(500));

                _statePublisher.SendReady += (s, e) =>
                 {
                     var state = new ClusterState()
                     {
                         AvailableWorkers = _workers.Count(),
                         Timestamp = DateTime.Now
                     };

                     _statePublisher.SendFrame(state.Serialize());
                 };

                using (_statePublisherPoller = new NetMQPoller { timer, _statePublisher })
                {
                    _statePublisherPoller.Run();
                }
            }
        }

        private void DoWork()
        {
            foreach (var work in _works.GetConsumingEnumerable())
            {

                _resetEvent.WaitOne();

                var worker = _workers.Dequeue();

                work.WorkerId = worker;

                _toWorkers.SendMoreFrame(worker.ToByteArray())
                        .SendMoreFrameEmpty()
                        .SendFrame(work.Serialize());
            }

        }

        public void Kill()
        {
            if (null == _workPoller) return;
            _workPoller.Stop();
        }

        public void Start()
        {
            using (_toGateway = new DealerSocket())
            {
                _toGateway.Options.Identity = _id.ToByteArray();
                _toGateway.Connect(_gatewayEndpoint);

                using (_toWorkers = new RouterSocket())
                {
                    _toWorkers.Options.Identity = _id.ToByteArray();
                    _toWorkers.Bind(_clusterEndpoint);

                    using (_workPoller = new NetMQPoller { _toGateway, _toWorkers })
                    {

                        _toGateway.ReceiveReady += (s, e) =>
                        {
                            var transportMessage = e.Socket.ReceiveMultipartMessage()
                                                           .GetMessageFromDealer<Work>();

                            var work = transportMessage.Message;

                            if (work.Status == WorkerStatus.Ask)
                            {
                                _works.Add(work);
                            }

                        };

                        _toWorkers.ReceiveReady += (s, e) =>
                        {
                            var transportMessage = e.Socket.ReceiveMultipartMessage()
                                                 .GetMessageFromRouter<Work>();

                            var work = transportMessage.Message;

                            if (work.Status == WorkerStatus.Ready)
                            {
                                _workers.Enqueue(new Guid(transportMessage.SenderId));
                            }
                            else if (work.Status == WorkerStatus.Finished)
                            {
                                _toGateway.SendFrame(transportMessage.MessageBytes);
                                _workers.Enqueue(new Guid(transportMessage.SenderId));
                            }

                            _resetEvent.Set();

                        };

                        //register to the gateway router...
                        _toGateway.SendFrame(Work.Ready.Serialize());

                        //...then start state publish
                        _statePublishProc = Task.Run(StatePublish).ConfigureAwait(false);

                        _workPoller.Run();

                    }
                }
            }

        }
    }
}
