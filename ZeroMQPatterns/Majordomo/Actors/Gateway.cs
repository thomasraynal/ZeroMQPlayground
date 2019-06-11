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

        private NetMQQueue<TransportMessage> _workQueue;
        private NetMQQueue<WorkerDescriptor> _workerQueue;
        private ResponseSocket _heartbeat;

        public IObservable<bool> IsConnected => Observable.Return(true);

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
                        var worker = _workerQueue.FirstOrDefault(w => w.WorkerId == heartbeatQuery.Descriptor.ActorId);

                        if (null != worker)
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

        public Task StartRouter()
        {

            _frontend = new RouterSocket();
            _backend = new RouterSocket();
            _workQueue = new NetMQQueue<TransportMessage>();
            _workerQueue = new NetMQQueue<WorkerDescriptor>();

            _frontend.Bind(_toClientsEndpoint);
            _backend.Bind(_toWorkersEndpoint);

            _poller = new NetMQPoller { _frontend, _backend, _workQueue, _workerQueue };

            _workQueue.ReceiveReady += (s, e) =>
            {

                if (_workerQueue.TryDequeue(out WorkerDescriptor worker, TimeSpan.FromMilliseconds(0)))
                {
                    var work = e.Queue.Dequeue();

                    work.WorkerId = worker.WorkerId;

                    _backend.SendMoreFrame(worker.WorkerId.ToByteArray())
                            .SendMoreFrameEmpty()
                            .SendFrame(work.Serialize());
                }
            };

            _workerQueue.ReceiveReady += (s, e) =>
            {

                if (_workQueue.TryDequeue(out TransportMessage work, TimeSpan.FromMilliseconds(0)))
                {
                    var worker = e.Queue.Dequeue();

                    work.WorkerId = worker.WorkerId;

                    _backend.SendMoreFrame(worker.WorkerId.ToByteArray())
                            .SendMoreFrameEmpty()
                            .SendFrame(work.Serialize());
                }

            };

            _frontend.ReceiveReady += (s, e) =>
            {
                var transportMessage = e.Socket.ReceiveMultipartMessage()
                                               .GetMessageFromRouter<TransportMessage>();

                var work = transportMessage.Message;

                work.ClientId = new Guid(transportMessage.SenderId);

                _workQueue.Enqueue(work);

            };

            _backend.ReceiveReady += (s, e) =>
            {
                var transportMessage = e.Socket.ReceiveMultipartMessage()
                                               .GetMessageFromRouter<TransportMessage>();

                var work = transportMessage.Message;

                if (work.State == WorkflowState.WorkerReady)
                {
                    _workerQueue.Enqueue(new WorkerDescriptor(transportMessage.SenderId));
                }
                if (work.State == WorkflowState.WorkFinished)
                {
                    _frontend.SendMoreFrame(work.ClientId.ToByteArray())
                             .SendMoreFrameEmpty()
                             .SendFrame(transportMessage.MessageBytes);

                    _workerQueue.Enqueue(new WorkerDescriptor(transportMessage.SenderId));
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

            _workerQueue.Dispose();
            _workQueue.Dispose();

            return Task.CompletedTask;
        }

    }
}
