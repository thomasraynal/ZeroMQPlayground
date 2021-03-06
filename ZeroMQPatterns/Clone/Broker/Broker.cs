﻿using NetMQ;
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
    public class Broker<TDto> : IHandleHeartbeat
        where TDto : ISubjectDto
    {
        private readonly BrokerConfiguration _brokerConfiguration;
        private readonly CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private ConfiguredTaskAwaitable _heartBeartProc;
        private PublisherSocket _publishStateUpdate;
        private SubscriberSocket _subscribeToUpdates;
        private RouterSocket _stateRequest;
        private readonly IUniqueEventIdProvider _uniqueEventIdProvider;
        private NetMQPoller _poller;
        private ResponseSocket _heartbeat;

        public List<ISequenceItem<TDto>> Updates { get; set; }

        public Broker(BrokerConfiguration brokerConfiguration)
        {
            _brokerConfiguration = brokerConfiguration;
            _cancel = new CancellationTokenSource();
            _uniqueEventIdProvider = _brokerConfiguration.Container.GetInstance<IUniqueEventIdProvider>();

            Updates = new List<ISequenceItem<TDto>>();
        }

        public void Start()
        {
            _publishStateUpdate = new PublisherSocket();
            _publishStateUpdate.Bind(_brokerConfiguration.PublishUpdatesEndpoint);

            _subscribeToUpdates = new SubscriberSocket();
            _subscribeToUpdates.SubscribeToAnyTopic();
            _subscribeToUpdates.Bind(_brokerConfiguration.SubscribeToUpdatesEndpoint);

            _stateRequest = new RouterSocket();
            _stateRequest.Bind(_brokerConfiguration.SendStateEndpoint);

            _workProc = Task.Run(DoStart, _cancel.Token).ConfigureAwait(false);
            _heartBeartProc = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);
        }

        public void Stop()
        {
            _cancel.Cancel();

            _poller.Stop();

            _heartbeat.Close();
            _heartbeat.Dispose();

            _publishStateUpdate.Close();
            _publishStateUpdate.Dispose();

            _subscribeToUpdates.Close();
            _subscribeToUpdates.Dispose();

            _stateRequest.Close();
            _stateRequest.Dispose();

        }

        public void HandleHeartbeat()
        {
            using (_heartbeat = new ResponseSocket(_brokerConfiguration.HeartbeatEndpoint))
            {

                while (!_cancel.IsCancellationRequested)
                {
                    var heartbeatQuery = _heartbeat.ReceiveFrameBytes()
                                                   .Deserialize<Heartbeat>();

                    if (_cancel.IsCancellationRequested) return;

                    _heartbeat.SendFrame(Heartbeat.Response.Serialize());

                }
            }
        }

        public void DoStart()
        {

            _poller = new NetMQPoller() { _subscribeToUpdates, _stateRequest };

            _subscribeToUpdates.ReceiveReady += (s, e) =>
            {
                var enveloppe = e.Socket.ReceiveMultipartMessage()
                                  .GetMessageFromPublisher<TDto>();

                var update = _brokerConfiguration.Container.GetInstance<ISequenceItem<TDto>>();

                update.Position = _uniqueEventIdProvider.Next();
                update.UpdateDto = enveloppe.Message;

                Updates.Add(update);

                _publishStateUpdate
                            .SendMoreFrame(update.UpdateDto.Subject.Serialize())
                            .SendFrame(update.Serialize());

            };

            _stateRequest.ReceiveReady += (s, e) =>
            {
                var enveloppe = e.Socket.ReceiveMultipartMessage()
                                        .GetMessageFromDealer<StateRequest>();

                var response = new StateReply<TDto>()
                {
                    Updates = Updates
                };

                _stateRequest.SendMoreFrame(enveloppe.SenderId)
                                 .SendMoreFrameEmpty()
                                 .SendFrame(response.Serialize());

            };

            _poller.Run();

        }

    }
}
