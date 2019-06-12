using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class Gateway : Actor, IGateway
    {

        private readonly string _toClientsEndpoint;
        private readonly string _toWorkersEndpoint;
        private readonly string _heartbeatEndpoint;
        private readonly CancellationTokenSource _cancel;

        private RouterSocket _backend;
        private RouterSocket _frontend;

        private ConfiguredTaskAwaitable _routerProc;
        private ConfiguredTaskAwaitable _handleHeartbeatProc;

        private NetMQPoller _poller;

        private ConcurrentDictionary<Guid,WorkerDescriptor> _registeredWorkers;
        private ConcurrentDictionary<Type, NetMQQueue<TransportMessage>> _workQueues;
        private ConcurrentDictionary<Type, NetMQQueue<WorkerDescriptor>> _workerQueues;

        private ResponseSocket _heartbeat;

        public IObservable<bool> IsConnected => Observable.Return(true);

        //todo : configuration descriptor
        public Gateway(string toClientsEndpoint, string toWorkersEndpoint, string heartbeatEndpoint)
        {
            _toClientsEndpoint = toClientsEndpoint;
            _toWorkersEndpoint = toWorkersEndpoint;
            _heartbeatEndpoint = heartbeatEndpoint;

            _cancel = new CancellationTokenSource();

        }

        public void HandleHeartbeat()
        {
            using (_heartbeat = new ResponseSocket(_heartbeatEndpoint))
            {

                while (!_cancel.IsCancellationRequested)
                {
                    var heartbeatQuery = _heartbeat.ReceiveFrameBytes()
                                                   .Deserialize<Heartbeat>();

                    if (heartbeatQuery.Descriptor.ActorType == ActorType.Worker)
                    {
                        if (_registeredWorkers.TryGetValue(heartbeatQuery.Descriptor.ActorId, out var worker))
                        {
                            worker.LastHeartbeat = DateTime.Now;
                        }
                    }

                    _heartbeat.SendFrame(this.GetHeartbeat(HeartbeatType.Pong).Serialize());

                }
            }
        }

        public override Task Start()
        {
            _handleHeartbeatProc = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);
            _routerProc = Task.Run(StartRouter, _cancel.Token).ConfigureAwait(false);

            return Task.CompletedTask;
        }

        private bool TryGetWorker(TransportMessage work, out WorkerDescriptor worker)
        {
            worker = null;

            if (_workerQueues.TryGetValue(work.CommandType, out var workerQueue))
            {
                while (workerQueue.TryDequeue(out WorkerDescriptor workerInternal, TimeSpan.Zero))
                {
                    //todo : config
                    if (workerInternal.IsAlive(TimeSpan.FromMilliseconds(5000)))
                    {
                        worker = workerInternal;
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnWorkAdded(TransportMessage work)
        {
       
            if(TryGetWorker(work, out WorkerDescriptor worker))
            {
                work.WorkerId = worker.WorkerId;

                _backend.SendMoreFrame(worker.WorkerId.ToByteArray())
                        .SendMoreFrameEmpty()
                        .SendFrame(work.Serialize());

            }
            else
            {
                var queue = _workQueues.GetOrAdd(work.CommandType, (type) =>
                {
                    return new NetMQQueue<TransportMessage>();
                });

                queue.Enqueue(work);
            }

        }

        private void OnWorkerAdded(WorkerDescriptor worker)
        {
     
            if (_workQueues.TryGetValue(worker.CommandType, out var workQueue))
            {
                if (workQueue.TryDequeue(out TransportMessage work, TimeSpan.Zero))
                {

                    work.WorkerId = worker.WorkerId;

                    _backend.SendMoreFrame(worker.WorkerId.ToByteArray())
                            .SendMoreFrameEmpty()
                            .SendFrame(work.Serialize());

                    return;
                }
            }

            var queue = _workerQueues.GetOrAdd(worker.CommandType, (type) =>
            {
                return new NetMQQueue<WorkerDescriptor>();
            });

            queue.Enqueue(worker);
        }

        public Task StartRouter()
        {

            _frontend = new RouterSocket();
            _backend = new RouterSocket();

            _workQueues = new ConcurrentDictionary<Type, NetMQQueue<TransportMessage>>();
            _workerQueues = new ConcurrentDictionary<Type, NetMQQueue<WorkerDescriptor>>();
            _registeredWorkers = new ConcurrentDictionary<Guid, WorkerDescriptor>();

            _frontend.Bind(_toClientsEndpoint);
            _backend.Bind(_toWorkersEndpoint);

            _poller = new NetMQPoller { _frontend, _backend };


            _frontend.ReceiveReady += (s, e) =>
            {
                var transportMessage = e.Socket.ReceiveMultipartMessage()
                                               .GetMessageFromDealer<TransportMessage>();

                var work = transportMessage.Message;

                work.ClientId = new Guid(transportMessage.SenderId);

                OnWorkAdded(work);

            };

            _backend.ReceiveReady += (s, e) =>
            {
                var enveloppe = e.Socket.ReceiveMultipartMessage()
                                               .GetMessageFromRouter<TransportMessage>();

                var work = enveloppe.Message;

                if (work.State == Workflow.WorkerReady)
                {
                    var worker = _registeredWorkers.GetOrAdd(new Guid(enveloppe.SenderId), (key) =>
                    {
                        return new WorkerDescriptor(enveloppe.SenderId, work.CommandType);
                    });

                    OnWorkerAdded(worker);
                }
                if (work.State == Workflow.WorkFinished)
                {
                    _frontend.SendMoreFrame(work.ClientId.ToByteArray())
                             .SendMoreFrameEmpty()
                             .SendFrame(enveloppe.MessageBytes);

                    var worker = _registeredWorkers.GetOrAdd(new Guid(enveloppe.SenderId), (key) =>
                    {
                        return new WorkerDescriptor(enveloppe.SenderId, work.CommandType);
                    });

                    OnWorkerAdded(worker);
                }

            };

            _poller.Run();

            return Task.CompletedTask;

        }

        public override Task Stop()
        {
            _cancel.Cancel();

            _poller.Stop();

            _heartbeat.Close();
            _heartbeat.Dispose();

            _frontend.Close();
            _frontend.Dispose();

            _backend.Close();
            _backend.Dispose();


            return Task.CompletedTask;
        }

    }
}
