using NetMQ;
using NetMQ.Sockets;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class Client : ActorBase, IClient
    {
        private string _gatewayEndpoint;
        private string _gatewayHeartbeatEndpoint;
        private ConfiguredTaskAwaitable _hearbeatProc;

        private readonly CancellationTokenSource _cancel;
        private readonly BehaviorSubject<bool> _isConnected;

        public Client(string gatewayEndpoint, string gatewayHeartbeatEndpoint)
        {
            _gatewayEndpoint = gatewayEndpoint;
            _gatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            _cancel = new CancellationTokenSource();
            _isConnected = new BehaviorSubject<bool>(false);

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

                        _isConnected.OnNext(response);
                    }

                    Thread.Sleep(hearbeatDelay.Milliseconds);
                }
            }
        }

        public Task<TResult> Send<TCommand, TResult>(TCommand command, TimeSpan maxResponseDelay)
            where TCommand : ICommand
            where TResult : ICommandResult
        {
            if (!_isConnected.Value) throw new Exception("lost connection to gateway");

            using (var client = new RequestSocket())
            {
                client.Options.Identity = Id.ToByteArray();
                client.Connect(_gatewayEndpoint);

                client.SendFrame(command.Serialize());

                if (client.TryReceiveFrameBytes(maxResponseDelay, out var responseBytes))
                {
                    var response = responseBytes.Deserialize<TResult>();
                    return Task.FromResult(response);
                }

            }

            throw new Exception("something wrong happened");
        }

        public override Task Start()
        {
            _hearbeatProc = Task.Run(() => DoHeartbeat(new[] { _gatewayHeartbeatEndpoint }, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000)), _cancel.Token)
                                .ConfigureAwait(false);

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _cancel.Cancel();

            _isConnected.OnCompleted();
            _isConnected.Dispose();

            return Task.CompletedTask;
        }

    }
}
