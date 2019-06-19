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

namespace ZeroMQPlayground.DynamicData.Domain
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

        private Proxy _xPubXSubProxy;
        private ResponseSocket _heartbeatSocket;
        private RouterSocket _stateRequestSocket;
        private SubscriberSocket _cacheSubscriberSocket;

        //todo: external cache storage
        private readonly List<TransportMessage> _cache;

        public Broker(string toPublisherEndpoint, string toSubscribersEndpoint, string stateOftheWorldEndpoint, string heartbeatEndpoint)
        {
            Id = Guid.NewGuid();

            _stateOfTheWorldEndpoint = stateOftheWorldEndpoint;
            _toPublishersEndpoint = toPublisherEndpoint;
            _toSubscribersEndpoint = toSubscribersEndpoint;
            _heartbeatEndpoint = heartbeatEndpoint;

            _cancel = new CancellationTokenSource();
            _cache = new List<TransportMessage>();

        }


        public List<TransportMessage> Cache => _cache;

        public Guid Id { get; }

        public bool IsStarted { get; private set; }

        public void HandleCache()
        {
            using ( _cacheSubscriberSocket = new SubscriberSocket())
            {
                _cacheSubscriberSocket.Bind("inproc://cache");
                _cacheSubscriberSocket.Connect(_toSubscribersEndpoint);
                _cacheSubscriberSocket.SubscribeToAnyTopic();

                while (!_cancel.IsCancellationRequested)
                {
                    var message = _cacheSubscriberSocket.ReceiveMultipartMessage();
                    var payload = message[1].Buffer.Deserialize<TransportMessage>();
                    _cache.Add(payload);
                }
            }
        }

        public void HandleHeartbeat()
        {
            using (_heartbeatSocket = new ResponseSocket(_heartbeatEndpoint))
            {
                while (!_cancel.IsCancellationRequested)
                {
                    var heartbeatQuery = _heartbeatSocket.ReceiveFrameBytes()
                                                   .Deserialize<Heartbeat>();

                    if (_cancel.IsCancellationRequested) return;

                    _heartbeatSocket.SendFrame(Heartbeat.Response.Serialize());

                }
            }
        }

        private void HandleStateOfTheWorldRequest()
        {
            using (_stateRequestSocket = new RouterSocket())
            {
                _stateRequestSocket.Bind(_stateOfTheWorldEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    //todo : api definition
                    var message = _stateRequestSocket.ReceiveMultipartMessage();
                    var sender = message[0].Buffer;
                    var request = message[1].Buffer.Deserialize<StateRequest>();

                    var response = new StateReply()
                    {
                        //Topic = request.Topic,
                        Subject = request.Subject
                    };

                    //if there is any cache at all
                    if (_cache.Count > 0)
                    {
                        if (request.Subject == string.Empty)
                        {
                            //todo: performance issues
                            response.Events = _cache;
                        }
                        else
                        {
                            response.Events = _cache.Where(ev => ev.Subject == request.Subject).ToList();
                        }
                    }

                    _stateRequestSocket.SendMoreFrame(sender)
                                 .SendFrame(response.Serialize());

                }
            }
        }

        private void HandleWork()
        {
            using (var stateUpdate = new XSubscriberSocket())
            {
                stateUpdate.Bind(_toPublishersEndpoint);

                using (var stateUpdatePublish = new XPublisherSocket())
                {
                    stateUpdatePublish.Bind(_toSubscribersEndpoint);
                    _xPubXSubProxy = new Proxy(stateUpdate, stateUpdatePublish);
                    _xPubXSubProxy.Start();

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
            _cacheHandlerProc = Task.Run(HandleCache, _cancel.Token).ConfigureAwait(false);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _cancel.Cancel();

            _xPubXSubProxy.Stop();

            _heartbeatSocket.Close();
            _heartbeatSocket.Dispose();

            _cacheSubscriberSocket.Close();
            _cacheSubscriberSocket.Dispose();

            _stateRequestSocket.Close();
            _stateRequestSocket.Dispose();

            IsStarted = false;

            return Task.CompletedTask;

        }
    }
}
