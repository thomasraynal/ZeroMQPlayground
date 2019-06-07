using NetMQ;
using NetMQ.Sockets;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.XPubXSub
{
    public abstract class MarketPricePublisherBase
    {
        public String Name { get; }
        public string Topic { get; }

        private string _brokerEndpoint;
        private CancellationToken _cancel;
        private ConfiguredTaskAwaitable _workProc;

        public MarketPricePublisherBase(String name, String topic, String brokerEndpoint, CancellationToken token)
        {
            Name = name;
            Topic = topic;

            _brokerEndpoint = brokerEndpoint;
            _cancel = token;
            _workProc = Task.Run(Work, _cancel).ConfigureAwait(false);
        }

        public abstract Price Next();

        private void Work()
        {
            using (var publisherSocket = new PublisherSocket())
            {
                publisherSocket.Connect(_brokerEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    var price = Next();

                    publisherSocket
                        .SendMoreFrame(Topic)
                        .SendFrame(price.Serialize());

                    Thread.Sleep(50);
                   
                }
            }
        }
    }
}
