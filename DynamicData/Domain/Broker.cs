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
    public class Broker
    {
        private readonly string _toPublishersEndpoint;
        private readonly string _toSubscribersEndpoint;
        private readonly string _stateOfTheWorldEndpoint;
        private readonly string _heartbeatEndpoint;

        private readonly CancellationTokenSource _cancel;
        private readonly ConfiguredTaskAwaitable _workProc;
        private readonly ConfiguredTaskAwaitable _heartbeartProc;
        private readonly ConfiguredTaskAwaitable _cacheHandlerProc;
        private readonly ConfiguredTaskAwaitable _stateOfTheWorldProc;
        private Proxy _proxy;
        private ResponseSocket _heartbeat;
        private RouterSocket _stateRequest;

        //todo: cross platform compliance....
        private readonly Dictionary<string, List<TransportMessage>> _cache;

        public Broker(string toPublisherEndpoint, string toSubscribersEndpoint, string stateOftheWorldEndpoint, string heartbeatEndpoint)
        {
            _stateOfTheWorldEndpoint = stateOftheWorldEndpoint;
            _toPublishersEndpoint = toPublisherEndpoint;
            _toSubscribersEndpoint = toSubscribersEndpoint;
            _heartbeatEndpoint = heartbeatEndpoint;

            _cancel = new CancellationTokenSource();

            //todo: threadsafe when state request
            _cache = new Dictionary<string, List<TransportMessage>>();

            //todo: proper cleanup - close sockets
            _workProc = Task.Run(Work, _cancel.Token).ConfigureAwait(false);
            _heartbeartProc = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);
            _stateOfTheWorldProc = Task.Run(HandleStateOfTheWorldRequest, _cancel.Token).ConfigureAwait(false);
            _cacheHandlerProc = Task.Run(HandleCache, _cancel.Token).ConfigureAwait(false);
        }

        public void Stop()
        {
            _cancel.Cancel();

            _proxy.Stop();

            _heartbeat.Close();
            _heartbeat.Dispose();

            _stateRequest.Close();
            _stateRequest.Dispose();

        }

        public Dictionary<string, List<TransportMessage>> Cache => _cache;

        public void HandleCache()
        {
            using (var cache = new SubscriberSocket())
            {
                cache.Bind("inproc://cache");
                cache.Connect(_toSubscribersEndpoint);
                cache.SubscribeToAnyTopic();

                while (!_cancel.IsCancellationRequested)
                {
                    var message = cache.ReceiveMultipartMessage();
                    var topic = message[0].Buffer.DeserializeString();
                    var payload = message[1].Buffer.Deserialize<TransportMessage>();

                    if (!_cache.ContainsKey(topic))
                    {
                        _cache[topic] = new List<TransportMessage>();
                    }

                    _cache[topic].Add(payload);
                }
            }
        }

        public void HandleHeartbeat()
        {
            using (_heartbeat = new ResponseSocket(_heartbeatEndpoint))
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

        private void HandleStateOfTheWorldRequest()
        {
            using (_stateRequest = new RouterSocket())
            {
                _stateRequest.Bind(_stateOfTheWorldEndpoint);

                while (!_cancel.IsCancellationRequested)
                {

                    //todo : api definition
                    var message = _stateRequest.ReceiveMultipartMessage();
                    var sender = message[0].Buffer;
                    var request = message[1].Buffer.Deserialize<StateRequest>();

                    var response = new StateReply()
                    {
                        Topic = request.Topic
                    };

                    if (request.Topic == string.Empty && _cache.Count > 0)
                    {
                        response.Events = _cache.Values.Aggregate((@events1, @events2) =>
                        {
                            return events1.Concat(events2).ToList();
                        });
                    }
                    else if (_cache.ContainsKey(request.Topic))
                    {
                        response.Events = _cache[request.Topic];
                    }

                    _stateRequest.SendMoreFrame(sender)
                                 .SendFrame(response.Serialize());

                }
            }
        }

        private void Work()
        {
            using (var stateUpdate = new XSubscriberSocket())
            {
                stateUpdate.Bind(_toPublishersEndpoint);

                using (var stateUpdatePublish = new XPublisherSocket())
                {
                    stateUpdatePublish.Bind(_toSubscribersEndpoint);
                    _proxy = new Proxy(stateUpdate, stateUpdatePublish);
                    _proxy.Start();

                }
            }
        }
    }
}
