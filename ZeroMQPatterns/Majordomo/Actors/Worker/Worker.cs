using NetMQ;
using NetMQ.Sockets;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public abstract class Worker<TCommand,TResult>: Actor, IWorker<TCommand, TResult>
        where TCommand : ICommand
        where TResult : ICommandResult
    {

        private readonly CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private ConfiguredTaskAwaitable _heartbeatProc;
        private IDisposable _disconnected;
        private readonly WorkerConfiguration _configuration;

        private readonly BehaviorSubject<bool> _isConnected;
        private RequestSocket _worker;

        public Worker(WorkerConfiguration configuration)
        {
            _configuration = configuration;
            _isConnected = new BehaviorSubject<bool>(false);

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
            _heartbeatProc = Task.Run(() => DoHeartbeat(new[] { _configuration.GatewayHeartbeatEndpoint }, _configuration.HearbeatDelay, _configuration.HearbeatMaxDelay), _cancel.Token)
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

                        if (_cancel.IsCancellationRequested) return;

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

        private TransportMessage CreateReadyEvent()
        {
            var response = new TransportMessage
            {
                CommandType = typeof(TCommand),

                State = Workflow.WorkerReady
            };

            return response;
        }

        private async Task<TransportMessage> CreateResponse(TransportMessage ask)
        {
            var response = new TransportMessage();

            var command = ask.Message.Deserialize<TCommand>();

            var commandResult = await Handle(command);

            commandResult.WorkerId = Id;

            response.Message = commandResult.Serialize();
            response.CommandType = typeof(TCommand);
            response.MessageType = typeof(TResult);
            response.WorkerId = ask.WorkerId;
            response.ClientId = ask.ClientId;
            response.CommandId = ask.CommandId;
            response.IsResponse = true;

            response.State = Workflow.WorkFinished;

            return response;

        }

        public async Task DoStart()
        {

            using (_worker = new RequestSocket())
            {
                _worker.Options.Identity = Id.ToByteArray();
                _worker.Connect(_configuration.GatewayEndpoint);

                while (!_isConnected.Value)
                {
                    Thread.Sleep(50);
                }

                _worker.SendFrame(CreateReadyEvent().Serialize());

                while (!_cancel.IsCancellationRequested)
                {
                    var work = _worker.ReceiveFrameBytes()
                                      .Deserialize<TransportMessage>();

                    if (_cancel.IsCancellationRequested) return;

                    var response = await CreateResponse(work);

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
