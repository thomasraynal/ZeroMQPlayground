using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class Broker
    {
        private readonly BrokerConfiguration _brokerConfiguration;
        private readonly CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private PublisherSocket _pushStateUpdate;
        private PullSocket _getUpdates;
        private RouterSocket _stateRequest;
        private NetMQPoller _poller;
        private IUniqueEventIdProvider _uniqueEventIdProvider;


        public List<MarketStateUpdate> MarketUpdates { get; set; }

        public Broker(BrokerConfiguration brokerConfiguration)
        {
            _brokerConfiguration = brokerConfiguration;
            _cancel = new CancellationTokenSource();
            _uniqueEventIdProvider = new DefaultUniqueEventIdProvider();

            MarketUpdates = new List<MarketStateUpdate>();
        }

        public void Start()
        {
            _workProc = Task.Run(DoStart, _cancel.Token).ConfigureAwait(false);
        }

        public void Stop()
        {
            _cancel.Cancel();
            _poller.Stop();
        }

        public void DoStart()
        {
            _pushStateUpdate = new PublisherSocket(_brokerConfiguration.PushStateUpdateEndpoint);
            _getUpdates = new PullSocket(_brokerConfiguration.GetUpdatesEndpoint);
            _stateRequest = new RouterSocket(_brokerConfiguration.StateRequestEndpoint);

            _poller = new NetMQPoller() { _getUpdates, _stateRequest };

            _getUpdates.ReceiveReady += (s, e) =>
            {
                var enveloppe = e.Socket.ReceiveMultipartMessage()
                                   .GetMessageFromDealer<MarketStateDto>();

                var update = enveloppe.Message.ToMarketStateUpdate(_uniqueEventIdProvider.Next());

                MarketUpdates.Add(update);

                _pushStateUpdate.SendFrame(update.Serialize());

            };

            _stateRequest.ReceiveReady += (s, e) =>
            {
                var enveloppe = e.Socket.ReceiveMultipartMessage()
                                        .GetMessageFromDealer<MarketStateRequest>();


                var updates = MarketUpdates.GroupBy(update => update.Asset)
                                           .Select(group => group.OrderBy(update => update.EventSequentialId).LastOrDefault())
                                           .Where(update => update != null)
                                           .ToList();

                var response = new MarketStateReply()
                {
                    Updates = updates
                };

                _stateRequest.SendMoreFrame(enveloppe.SenderId)
                                 .SendMoreFrameEmpty()
                                 .SendFrame(response.Serialize());

            };

            _poller.Run();

        }

    }
}
