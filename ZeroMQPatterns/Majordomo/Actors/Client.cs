using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class Client : Actor, IClient
    {
        private string _gatewayEndpoint;
        private string _gatewayHeartbeatEndpoint;
        private ConfiguredTaskAwaitable _hearbeatProc;
        private ConfiguredTaskAwaitable _responsePollerProc;
        private DealerSocket _client;
        private NetMQPoller _poller;
        private readonly CancellationTokenSource _cancel;
        private readonly BehaviorSubject<bool> _isConnected;

        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResult>> _commandResults;

        public Client(string gatewayEndpoint, string gatewayHeartbeatEndpoint)
        {
            _gatewayEndpoint = gatewayEndpoint;
            _gatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            _cancel = new CancellationTokenSource();
            _isConnected = new BehaviorSubject<bool>(false);

            _commandResults = new Dictionary<Guid, TaskCompletionSource<ICommandResult>>();

        }

        public IObservable<bool> IsConnected
        {
            get
            {
                return _isConnected.AsObservable();
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

        //todo: handle timeout
        public Task<TResult> Send<TCommand, TResult>(TCommand command)
            where TCommand : ICommand
            where TResult : ICommandResult
        {
            if (!_isConnected.Value) throw new Exception("lost connection to gateway");

            var message = new TransportMessage()
            {
                CommandId = Guid.NewGuid(),
                Message = command.Serialize(),
                CommandType = typeof(TCommand)
            };

            var task = new TaskCompletionSource<ICommandResult>();
            _commandResults.Add(message.CommandId, task);

            _client.SendFrame(message.Serialize());

            return task.Task.ContinueWith(result => (TResult)result.Result);

        }

        private void DoStart()
        {
            _poller.Run();
        }

        public override Task Start()
        {
            _hearbeatProc = Task.Run(() => DoHeartbeat(new[] { _gatewayHeartbeatEndpoint }, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(1000)), _cancel.Token)
                                .ConfigureAwait(false);


            _client = new DealerSocket();
            _client.Options.Identity = Id.ToByteArray();
            _client.Connect(_gatewayEndpoint);

            _poller = new NetMQPoller { _client };

            _client.ReceiveReady += (s, e) =>
            {
                var enveloppe = e.Socket.ReceiveMultipartMessage()
                                        .GetMessageFromDealer<TransportMessage>();

                var response = enveloppe.Message;

                if (response.IsResponse && _commandResults.TryGetValue(response.CommandId, out var task))
                {
                    var commandResult = response.Message.Deserialize(response.MessageType);

                    task.SetResult(commandResult as ICommandResult);
                }
            };

            _responsePollerProc = Task.Run(DoStart, _cancel.Token)
                                      .ConfigureAwait(false);


            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _cancel.Cancel();

            _poller.Stop();

            _client.Disconnect(_gatewayEndpoint);
            _client.Close();
            _client.Dispose();

            _isConnected.OnCompleted();
            _isConnected.Dispose();

            return Task.CompletedTask;
        }

    }
}
