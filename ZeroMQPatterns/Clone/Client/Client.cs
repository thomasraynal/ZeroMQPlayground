using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class Client<TDto> where TDto : ISubjectDto
    {
        private readonly ClientConfiguration _configuration;
        private readonly CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private DealerSocket _getMarketState;
        private SubscriberSocket _getMarketUpdates;
        private PublisherSocket _publishMarketUpdates;

        public SortedList<long, ISequenceItem<TDto>> Updates { get; set; }
        public Guid Id { get; }
        public String Name { get; }

        public Client(ClientConfiguration configuration)
        {
            _configuration = configuration;

            Updates = new SortedList<long, ISequenceItem<TDto>>();

            Id = Guid.NewGuid();
            Name = _configuration.Name;

            _cancel = new CancellationTokenSource();

        }
        public void Push(ISubjectDto dto)
        {
            _publishMarketUpdates
                .SendMoreFrame(dto.Subject.Serialize())
                .SendFrame(dto.Serialize());
        }

        private Task Synchronize()
        {
            _getMarketState.SendFrame(StateRequest.Default.Serialize());

            var enveloppe = _getMarketState.ReceiveMultipartMessage()
                                           .GetMessageFromPublisher<StateReply<TDto>>();

            foreach (var update in enveloppe.Message.Updates)
            {
                Updates.Add(update.Position, update);
            }

            return Task.CompletedTask;
        }

        public async Task Start()
        {
            var cancel = new CancellationTokenSource(_configuration.RouterConnectionTimeout);

            _getMarketState = new DealerSocket();
            _getMarketState.Connect(_configuration.GetStateEndpoint);
            _getMarketState.Options.Identity = Id.ToByteArray();

            _getMarketUpdates = new SubscriberSocket();
            _getMarketUpdates.SubscribeToAnyTopic();
            _getMarketUpdates.Connect(_configuration.SubscribeToUpdatesEndpoint);

            _publishMarketUpdates = new PublisherSocket();
            _publishMarketUpdates.Connect(_configuration.PublishUpdateEndpoint);

            _workProc = Task.Run(DoStart, _cancel.Token).ConfigureAwait(false);

            await Task.Delay(200);

            await Task.Run(Synchronize, cancel.Token);

        }

        public void Stop()
        {
            _cancel.Cancel();

            _getMarketState.Close();
            _getMarketState.Dispose();

            _getMarketUpdates.Close();
            _getMarketUpdates.Dispose();
        }

        private void DoStart()
        {
            while (!_cancel.IsCancellationRequested)
            {
                var enveloppe = _getMarketUpdates.ReceiveMultipartMessage()
                                                 .GetMessageFromPublisher<ISequenceItem<TDto>>();

                Updates.Add(enveloppe.Message.Position, enveloppe.Message);

            }

        }

    }
}
