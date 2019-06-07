using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.XPubXSub
{
    public class Broker
    {
        private readonly string _toPublisherEndpoint;
        private readonly string _toSubscriberEndpoint;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _workProc;
        private Proxy _proxy;

        public Broker(string toPublisherEndpoint, string toSubscriberEndpoint, CancellationToken token)
        {
            _toPublisherEndpoint = toPublisherEndpoint;
            _toSubscriberEndpoint = toSubscriberEndpoint;
            _cancel = token;
            _workProc = Task.Run(Work, _cancel).ConfigureAwait(false);
        }

        public void Kill()
        {
            _proxy.Stop();
        }

        private void Work()
        {
            using (var subscriberSocket = new XSubscriberSocket())
            {
                subscriberSocket.Bind(_toPublisherEndpoint);

                using (var publisherSocket = new XPublisherSocket())
                {
                    publisherSocket.Bind(_toSubscriberEndpoint);

                    _proxy = new Proxy(subscriberSocket, publisherSocket);
                    _proxy.Start();
                }
            }
        }
    }
}
