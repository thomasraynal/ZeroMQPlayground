using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public class Broker : IActor
    {
        private readonly string _toPublishersEndpoint;
        private readonly string _toSubscribersEndpoint;
        private readonly string _stateOfTheWorldEndpoint;
        private readonly string _heartbeatEndpoint;

        private readonly CancellationTokenSource _cancel;

        private ConfiguredTaskAwaitable _workProc;
        private ConfiguredTaskAwaitable _heartbeartProc;
        private ConfiguredTaskAwaitable _cacheHandlerProc;
        private ConfiguredTaskAwaitable _stateOfTheWorldProc;

        private NetMQPoller _poller;
        private ResponseSocket _heartbeatSocket;
        private RouterSocket _stateRequestSocket;

        private readonly IEventCache _cache;
        private readonly ISerializer _serializer;

        public Broker(string toPublisherEndpoint, 
            string toSubscribersEndpoint, 
            string stateOftheWorldEndpoint, 
            string heartbeatEndpoint, 
            IEventCache cache,
            ISerializer serializer)
        {
            Id = Guid.NewGuid();

            _cache = cache;
            _serializer = serializer;
            _stateOfTheWorldEndpoint = stateOftheWorldEndpoint;
            _toPublishersEndpoint = toPublisherEndpoint;
            _toSubscribersEndpoint = toSubscribersEndpoint;
            _heartbeatEndpoint = heartbeatEndpoint;

            _cancel = new CancellationTokenSource();

        }

        public Guid Id { get; }
        public bool IsStarted { get; private set; }

        public void HandleHeartbeat()
        {
            using (_heartbeatSocket = new ResponseSocket(_heartbeatEndpoint))
            {
                while (!_cancel.IsCancellationRequested)
                {
                    var messageBytes = _heartbeatSocket.ReceiveFrameBytes();

                    if (_cancel.IsCancellationRequested) return;

                    _heartbeatSocket.SendFrame(_serializer.Serialize(Heartbeat.Response));

                }
            }
        }

        private async Task HandleStateOfTheWorldRequest()
        {
            using (_stateRequestSocket = new RouterSocket())
            {
                _stateRequestSocket.Bind(_stateOfTheWorldEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    var message = _stateRequestSocket.ReceiveMultipartMessage();
                    var sender = message[0].Buffer;
                    var request = _serializer.Deserialize<StateRequest>(message[1].Buffer);

                    var stream = await _cache.GetStreamBySubject(request.Subject);

                    var response = new StateReply()
                    {
                        Subject = request.Subject,
                        Events = stream.Select(ev => _serializer.Deserialize<TransportMessage>(ev.Message)).ToList()
                    };

                    _stateRequestSocket.SendMoreFrame(sender)
                                 .SendFrame(_serializer.Serialize(response));

                }
            }
        }

        private void HandleWork()
        {
            using (var stateUpdate = new SubscriberSocket())
            {
                stateUpdate.SubscribeToAnyTopic();
                stateUpdate.Bind(_toPublishersEndpoint);

                using (var stateUpdatePublish = new PublisherSocket())
                {
                    stateUpdatePublish.Bind(_toSubscribersEndpoint);

                    //protractor?
                    stateUpdate.ReceiveReady += async (s, e) =>
                            {
                                //todo: perf - just forwarding + eventid
                                var message = e.Socket.ReceiveMultipartMessage();

                                var subject = message[0].ConvertToString();
                                var payload = message[1];

                                await _cache.AppendToStream(subject, payload.Buffer);

                                stateUpdatePublish.SendMoreFrame(message[0].Buffer)
                                                  .SendFrame(payload.Buffer);
                            };

                    using (_poller = new NetMQPoller { stateUpdate, stateUpdatePublish })
                    {
                        _poller.Run();
                    }
                }
            }
        }

        public Task Start()
        {
            if (IsStarted) throw new InvalidOperationException($"{nameof(Broker)} is already started");

            IsStarted = true;

            _workProc = Task.Run(HandleWork, _cancel.Token).ConfigureAwait(false);
            _heartbeartProc = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);
            _stateOfTheWorldProc = Task.Run(HandleStateOfTheWorldRequest, _cancel.Token).ConfigureAwait(false);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _cancel.Cancel();

            _poller.Stop();

            _heartbeatSocket.Close();
            _heartbeatSocket.Dispose();

            _stateRequestSocket.Close();
            _stateRequestSocket.Dispose();

            IsStarted = false;

            return Task.CompletedTask;

        }
    }
}
