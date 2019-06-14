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

namespace ZeroMQPlayground.ZeroMQPatterns.Expresso
{
    public class Broker
    {
        private readonly string _toPublisherEndpoint;
        private readonly string _toSubscribersEndpoint;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _workProc;
        private readonly Dictionary<string, byte[]> _cache; 
        private NetMQPoller _poller;

        public Broker(string toPublisherEndpoint, string toSubscriberEndpoint, CancellationToken token)
        {
            _toPublisherEndpoint = toPublisherEndpoint;
            _toSubscribersEndpoint = toSubscriberEndpoint;
            _cache = new Dictionary<string, byte[]>();
            _cancel = token;
            _workProc = Task.Run(Work, _cancel).ConfigureAwait(false);
        }

        public void Kill()
        {
            _poller.Stop();
        }

        private void Work()
        {
            using (var subscriberSocket = new SubscriberSocket())
            {
                subscriberSocket.Options.ReceiveHighWatermark = 1000;
                subscriberSocket.SubscribeToAnyTopic();
                subscriberSocket.Connect(_toPublisherEndpoint);
          

                using (var publisherSocket = new XPublisherSocket())
                {
                    publisherSocket.Options.XPubVerbose = true;
                    publisherSocket.Bind(_toSubscribersEndpoint);

                    using (_poller = new NetMQPoller {new NetMQTimer(TimeSpan.FromMilliseconds(1)), subscriberSocket, publisherSocket })
                    {

                    
                        subscriberSocket.ReceiveReady += (s, e) =>
                        {
                            var message = e.Socket.ReceiveMultipartBytes();
                            var topic = Encoding.UTF8.GetString(message[0]);
                            var payload = message[1];

                            _cache[topic] = payload;

                            publisherSocket.SendMultipartBytes(message);

                        };

                        publisherSocket.ReceiveReady += (s, e) =>
                        {
                            var message = e.Socket.ReceiveFrameBytes();
                            var isSub = message[0] == 1;
                            var topic = Encoding.UTF8.GetString(message.Skip(1).ToArray());

                            if (_cache.ContainsKey(topic))
                            {
                                publisherSocket.SendMoreFrame(Encoding.UTF8.GetBytes(topic)).SendFrame(_cache[topic]);
                            }

                        };

                        _poller.Run();
                    }
                }
            }
        }
    }
}
