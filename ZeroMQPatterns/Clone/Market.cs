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
        private PushSocket _pushMarketStateUpdate;
        private DealerSocket _getMarketState;
        private SubscriberSocket _getMarketStateUpdate;

        public SortedList<long, MarketStateUpdate> Updates { get; set; }
        public Guid Id { get; }

        public Market(MarketConfiguration configuration)
        {
            _configuration = configuration;

            Updates = new SortedList<long, MarketStateUpdate>();

            Id = Guid.NewGuid();

            _cancel = new CancellationTokenSource();

        }
        public void Push(MarketStateDto update)
        {
            _pushMarketStateUpdate.SendFrame(update.Serialize());
        }

        public Task Synchronize()
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
            _workProc = Task.Run(DoStart, _cancel.Token).ConfigureAwait(false);

            var cancel = new CancellationTokenSource(_configuration.RouterConnectionTimeout);

            await Task.Run(Synchronize, cancel.Token);

        }

        public void Stop()
        {
            _cancel.Cancel();

            _pushMarketStateUpdate.Close();
            _pushMarketStateUpdate.Dispose();

            _getMarketState.Close();
            _getMarketState.Dispose();

            _getMarketStateUpdate.Close();
            _getMarketStateUpdate.Dispose();
        }

        public void DoStart()
        {
            _pushMarketStateUpdate = new PushSocket(_configuration.PushStateUpdateEndpoint);
            _pushMarketStateUpdate.Options.Identity = Id.ToByteArray();

            _getMarketState = new DealerSocket(_configuration.PushStateUpdateEndpoint);
            _getMarketState.Options.Identity = Id.ToByteArray();

            _getMarketStateUpdate = new SubscriberSocket(_configuration.GetUpdatesEndpoint);
            _getMarketStateUpdate.Options.Identity = Id.ToByteArray();

            _getMarketStateUpdate.SubscribeToAnyTopic();

            while (!_cancel.IsCancellationRequested)
            {
                var enveloppe = _getMarketStateUpdate.ReceiveMultipartMessage()
                                     .GetMessageFromPublisher<MarketStateUpdate>();

                Updates.Add(enveloppe.Message.EventSequentialId, enveloppe.Message);

            }

        }

    }
}
