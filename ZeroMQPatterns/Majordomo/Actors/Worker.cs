using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public abstract class Worker<TCommand,TResult>: Actor, IWorker<TCommand, TResult>
    {
        private readonly string _gatewayEndpoint;
        private readonly string _gatewayHeartbeatEndpoint;

        private readonly CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private ConfiguredTaskAwaitable _heartbeatProc;
        private IDisposable _disconnected;

        private readonly BehaviorSubject<bool> _isConnected;
        private readonly Random _rand;
        private RequestSocket _worker;

        public Worker(string gatewayEndpoint, string gatewayHeartbeatEndpoint)
        {
            _gatewayEndpoint = gatewayEndpoint;
            _gatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            _isConnected = new BehaviorSubject<bool>(false);

            _rand = new Random();
            _cancel = new CancellationTokenSource();

        }

        public IObservable<bool> IsConnected
        {
            get
            {
                return _isConnected.AsObservable();
            }
        }

        public override Task Start()
        {
            _heartbeatProc = Task.Run(() => DoHeartbeat(new[] { _gatewayHeartbeatEndpoint }, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000)), _cancel.Token)
                         .ConfigureAwait(false);

            _disconnected = IsConnected
                    .Buffer(2, 1)
                    .Where(buffer => buffer.Count > 1 && buffer[0] && !buffer[1])
                    .Subscribe(_ =>
                    {
                        StartWorkProc();
                    });

            StartWorkProc();

            return Task.CompletedTask;
        }

        public void DoHeartbeat(string[] targets, TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay)
        {
            while (!_cancel.IsCancellationRequested)
            {
                foreach (var target in targets)
                {
                    using (var heartbeat = new RequestSocket(target))
                    {
                        heartbeat.SendFrame(this.GetHeartbeat(HeartbeatType.Ping)
                                                .Serialize());

                        var response = heartbeat.TryReceiveFrameBytes(hearbeatMaxDelay, out var responseBytes);

                        _isConnected.OnNext(response);
                    }

                    Thread.Sleep(hearbeatDelay.Milliseconds);
                }
            }
        }

        private void StartWorkProc()
        {
            if (null != _worker)
            {
                _worker.Close();
                _worker.Dispose();
            }

            _workProc = Task.Run(DoStart, _cancel.Token).ConfigureAwait(false);
        }

        public async Task<TransportMessage> CreateResponse(TransportMessage ask)
        {
            var response = new TransportMessage();

            var command = ask.Message.Deserialize<TCommand>();

            var commandResult = await Handle(command);

            response.Message = commandResult.Serialize();
            response.MessageType = typeof(TResult);
            response.WorkerId = ask.WorkerId;
            response.ClientId = ask.ClientId;
            response.CommandId = ask.CommandId;
            response.IsResponse = true;

            response.State = WorkflowState.WorkFinished;

            return response;

        }

        public async Task DoStart()
        {

            using (_worker = new RequestSocket())
            {
                _worker.Options.Identity = Id.ToByteArray();
                _worker.Connect(_gatewayEndpoint);

                while (!_isConnected.Value)
                {
                    Thread.Sleep(50);
                }

                _worker.SendFrame(TransportMessage.Ready.Serialize());

                while (!_cancel.IsCancellationRequested)
                {
                    var work = _worker.ReceiveFrameBytes()
                                      .Deserialize<TransportMessage>();

                    if (_cancel.IsCancellationRequested) return;

                    var response = await CreateResponse(work);

                    response.WorkerId = work.WorkerId;

                    _worker.SendFrame(response.Serialize());
                }
            }
        }

        public override Task Stop()
        {
            _cancel.Cancel();

            _isConnected.OnCompleted();
            _disconnected.Dispose();

            _worker.Close();
            _worker.Dispose();

            return Task.CompletedTask;
        }

        public abstract Task<TResult> Handle(TCommand command);
    }
}
