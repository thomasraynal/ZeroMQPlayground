using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class Gateway : ActorBase, IGateway
    {

        private readonly string _frontendRouterEndpoint;
        private readonly string _backendRouterEndpoint;
        private readonly string _heartbeatEndpoint;
        private readonly CancellationTokenSource _cancel;

        private RouterSocket _backend;
        private RouterSocket _frontend;

        private ConfiguredTaskAwaitable _endpointProc;
        private ConfiguredTaskAwaitable _handleHeartbeatProc;

        private NetMQPoller _poller;

        private List<Guid> _deadWorkers;
        private NetMQQueue<Work> _workQueue;
        private NetMQQueue<Guid> _workerQueue;
        private ResponseSocket _heartbeat;

        public IObservable<bool> IsConnected => Observable.Return(true);

        public Gateway(string frontendEndpoint, string backendEnpoint, string heartbeatEndpoint)
        {
            _frontendRouterEndpoint = frontendEndpoint;
            _backendRouterEndpoint = backendEnpoint;
            _heartbeatEndpoint = heartbeatEndpoint;

            _deadWorkers = new List<Guid>();

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

                    _heartbeat.SendFrame(this.GetHeartbeat(HeartbeatType.Pong).Serialize());

                }
            }
        }

        public void DoHeartbeat(string[] targets, TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay)
        {
            while (!_cancel.IsCancellationRequested)
            {
                foreach (var target in targets)
                {
                    using (var heartbeat = new RequestSocket(target))
                    {
                        heartbeat.SendFrame(this.GetHeartbeat(HeartbeatType.Ping).Serialize());

                        var response = heartbeat.TryReceiveFrameBytes(hearbeatMaxDelay, out var responseBytes);

                        if (!response)
                        {
                            todo
                            //var responseMessage = responseBytes.Deserialize<Heartbeat>();
                            //_deadWorkers.Add(responseMessage.);
                        }
                    }

                    Thread.Sleep(hearbeatDelay.Milliseconds);
                }
            }
        }

        public override Task Start()
        {
            _handleHeartbeatProc = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);
            _endpointProc = Task.Run(StartRouter, _cancel.Token).ConfigureAwait(false);

            return Task.CompletedTask;
        }

        public Task StartRouter()
        {

            _frontend = new RouterSocket();
            _backend = new RouterSocket();
            _workQueue = new NetMQQueue<Work>();
            _workerQueue = new NetMQQueue<Guid>();

            _frontend.Bind(_frontendRouterEndpoint);
            _backend.Bind(_backendRouterEndpoint);

            _poller = new NetMQPoller { _frontend, _backend, _workQueue, _workerQueue };

            _workQueue.ReceiveReady += (s, e) =>
            {

                if (_workerQueue.TryDequeue(out Guid worker, TimeSpan.FromMilliseconds(0)))
                {
                    var work = e.Queue.Dequeue();

                    work.WorkerId = worker;

                    _backend.SendMoreFrame(worker.ToByteArray())
                            .SendMoreFrameEmpty()
                            .SendFrame(work.Serialize());
                }
            };

            _workerQueue.ReceiveReady += (s, e) =>
            {

                if (_workQueue.TryDequeue(out Work work, TimeSpan.FromMilliseconds(0)))
                {
                    var worker = e.Queue.Dequeue();

                    work.WorkerId = worker;

                    _backend.SendMoreFrame(worker.ToByteArray())
                            .SendMoreFrameEmpty()
                            .SendFrame(work.Serialize());
                }

            };

            _frontend.ReceiveReady += (s, e) =>
            {
                var transportMessage = e.Socket.ReceiveMultipartMessage()
                                               .GetMessageFromRouter<Work>();

                var work = transportMessage.Message;

                work.ClientId = new Guid(transportMessage.SenderId);

                if (work.MessageType == MessageType.Ask)
                {
                    _workQueue.Enqueue(work);
                }

            };

            _backend.ReceiveReady += (s, e) =>
            {
                var transportMessage = e.Socket.ReceiveMultipartMessage()
                                               .GetMessageFromRouter<Work>();

                var work = transportMessage.Message;

                if (work.MessageType == MessageType.Ready)
                {
                    _workerQueue.Enqueue(new Guid(transportMessage.SenderId));
                }
                if (work.MessageType == MessageType.Finished)
                {
                    _frontend.SendMoreFrame(work.ClientId.ToByteArray())
                             .SendMoreFrameEmpty()
                             .SendFrame(transportMessage.MessageBytes);

                    _workerQueue.Enqueue(new Guid(transportMessage.SenderId));
                }

            };

            _poller.Run();

            return Task.CompletedTask;

        }

        public override Task Stop()
        {
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
