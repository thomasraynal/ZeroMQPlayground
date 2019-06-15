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
    public class Market
    {
        private readonly MarketConfiguration _configuration;
        private readonly CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private DealerSocket _getMarketState;
        private SubscriberSocket _getMarketUpdates;

        public SortedList<long, MarketStateUpdate> Updates { get; set; }
        public Guid Id { get; }
        public String Name { get; }

        public Market(MarketConfiguration configuration)
        {
            _configuration = configuration;

            Updates = new SortedList<long, MarketStateUpdate>();

            Id = Guid.NewGuid();
            Name = _configuration.Name;

            _cancel = new CancellationTokenSource();

        }
        public void Push()
        {
            var update = MarketStateDto.Random(Name);

            using (var sender = new PushSocket(_configuration.PushMarketUpdateEndpoint))
            {
                sender.SendFrame(update.Serialize());
                Thread.Sleep(200);
            }
        }

        private Task Synchronize()
        {
            _getMarketState.SendFrame(MarketStateRequest.Default.Serialize());

            var enveloppe = _getMarketState.ReceiveMultipartMessage()
                                           .GetMessageFromPublisher<MarketStateReply>();

            foreach (var update in enveloppe.Message.Updates)
            {
                Updates.Add(update.EventSequentialId, update);
            }

            return Task.CompletedTask;
        }

        public async Task Start()
        {
            var cancel = new CancellationTokenSource(_configuration.RouterConnectionTimeout);

            _getMarketState = new DealerSocket();
            _getMarketState.Connect(_configuration.GetMarketStateEndpoint);
            _getMarketState.Options.Identity = Id.ToByteArray();

            _getMarketUpdates = new SubscriberSocket();
            _getMarketUpdates.Connect(_configuration.GetMarketUpdatesEndpoint);
            _getMarketUpdates.Options.Identity = Id.ToByteArray();

            _getMarketUpdates.SubscribeToAnyTopic();

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
                                     .GetMessageFromPublisher<MarketStateUpdate>();

                Updates.Add(enveloppe.Message.EventSequentialId, enveloppe.Message);

            }

        }

    }
}
