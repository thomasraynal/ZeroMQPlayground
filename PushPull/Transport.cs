using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using StructureMap;
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
        private readonly IBusConfiguration _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IDirectory _directory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResult>> _commandResults;
        private object _locker = new object();

        public Transport(IBusConfiguration configuration, IContainer container, IDirectory directory, JsonSerializerSettings settings)
        {
            _configuration = configuration;
            _cancellationTokenSource = new CancellationTokenSource();
            _messageDispatcher = container.GetInstance<IMessageDispatcher>();
            _commandResults = new Dictionary<Guid, TaskCompletionSource<ICommandResult>>();
            _directory = directory;
            _jsonSerializerSettings = settings;

            Task.Run(Pull, _cancellationTokenSource.Token);

        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Pull()
        {
            using (var receiver = new PullSocket(_configuration.Endpoint))
            {
                while (true)
                {

                    var messageBytes = receiver.ReceiveFrameBytes();
                    var message = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(messageBytes), _jsonSerializerSettings);

                    if (message.IsResponse && _commandResults.TryGetValue(message.MessageId, out var task))
                    {
                        var commandResult = JsonConvert.DeserializeObject(Encoding.UTF32.GetString(message.Message), message.MessageType, _jsonSerializerSettings);

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
                lock (_locker)
                {

                    using (var sender = new PushSocket(peer.Endpoint))
                    {
                        var message = new TransportMessage()
                        {
                            IsResponse = false,
                            Message = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(@event, _jsonSerializerSettings)),
                            MessageType = @event.GetType()
                        };

                        var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(message, _jsonSerializerSettings));

                        sender.SendFrame(msg);


                    }
                }
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
