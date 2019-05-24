using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Transport : ITransport
    {
        private readonly IBus _bus;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IDirectory _directory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResult>> _commandResults;
        private object _locker = new object();

        public Transport(IBus bus, IContainer container, IDirectory directory, JsonSerializerSettings settings)
        {
            _bus = bus;
            _cancellationTokenSource = new CancellationTokenSource();
            _messageDispatcher = container.GetInstance<IMessageDispatcher>();
            _commandResults = new Dictionary<Guid, TaskCompletionSource<ICommandResult>>();
            _directory = directory;
            _jsonSerializerSettings = settings;

        }


        public async Task Start()
        {
            Task.Run(Pull, _cancellationTokenSource.Token);

            await _directory.Start();

        }

        public void Pull()
        {
            using (var receiver = new PullSocket(_bus.Self.Endpoint))
            {
                while (true)
                {

                    var messageBytes = receiver.ReceiveFrameBytes();
                    var message = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(messageBytes), _jsonSerializerSettings);

                    if (message.IsResponse && _commandResults.TryGetValue(message.CommandId, out var task))
                    {
                        var commandResult = JsonConvert.DeserializeObject(Encoding.UTF32.GetString(message.Message), message.MessageType, _jsonSerializerSettings);

                        task.SetResult(commandResult as ICommandResult);
                    }

                    _messageDispatcher.Dispatch(message);

                }
            }
        }

        public void Send(IEvent @event)
        {
            var matched = _directory.GetMatchedPeers(@event).ToList();

            Send(@event, matched, Guid.Empty);
        }

        public void Send(ICommandResult @event, IPeer peer)
        {
            Send(@event, new[] { peer }.ToList(), @event.CommandId, true);
        }

        private void Send(IEvent @event, List<IPeer> peers, Guid commandId, bool isResponse = false)
    {
            foreach (var peer in peers)
            {
            
                    using (var sender = new PushSocket(peer.Endpoint))
                    {
                        var message = new TransportMessage()
                        {
                            CommandId = commandId,
                            IsResponse = isResponse,
                            Message = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(@event, _jsonSerializerSettings)),
                            MessageType = @event.GetType()
                        };

                        var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(message, _jsonSerializerSettings));

                        sender.SendFrame(msg);

                        Task.Delay(200).Wait();

                    }
            }
        }

        public Task<ICommandResult> Send(ICommand command, IPeer peer)
        {
            var task = new TaskCompletionSource<ICommandResult>();

            _commandResults.Add(command.CommandId, task);

            Send(command, new[] { peer }.ToList(), command.CommandId);

            return task.Task;
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _messageDispatcher.Stop();
        }
    }
}
