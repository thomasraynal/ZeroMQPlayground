using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Transport : ITransport, IDisposable
    {
        private readonly IBus _bus;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IDirectory _directory;
        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResult>> _commandResults;

        public Transport(IBus bus, IDirectory directory)
        {
            _bus = bus;
            _cancellationTokenSource = new CancellationTokenSource();
            _messageDispatcher = _bus.Container.GetInstance<IMessageDispatcher>();
            _commandResults = new Dictionary<Guid, TaskCompletionSource<ICommandResult>>();
            _directory = directory;

            Task.Run(Pull, _cancellationTokenSource.Token);

        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Pull()
        {
            using (var receiver = new PullSocket(_bus.Self.Endpoint))
            {
                while (true)
                {

                    var messageBytes = receiver.ReceiveFrameBytes();
                    var message = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(messageBytes));

                    if (message.IsResponse && _commandResults.TryGetValue(message.MessageId, out var task))
                    {
                        var commandResult = JsonConvert.DeserializeObject(Encoding.UTF32.GetString(message.Message), message.MessageType);

                        task.SetResult(message as ICommandResult);
                    }

                    _messageDispatcher.Dispatch(message);

                }
            }
        }

        public void Send(IEvent @event)
        {
            var matched = _directory.GetMatchedPeers(@event);

            foreach(var peer in matched)
            {
                Task.Run(async () =>
                {
                    using (var sender = new PushSocket(peer.Endpoint))
                    {
                        var message = new TransportMessage()
                        {
                            IsResponse = false,
                            Message = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(@event)),
                            MessageType = @event.GetType()
                        };

                        var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(message));

                        sender.SendFrame(msg);

                        await Task.Delay(100);
                    }
                });
            }
        }

        public Task<ICommandResult> Send(ICommand command)
        {
            var msgId = Guid.NewGuid();
            var task = new TaskCompletionSource<ICommandResult>();

            _commandResults.Add(msgId, task);

            Send(command);

            return task.Task;
        }

    }
}
