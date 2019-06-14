using NetMQ;
using NetMQ.Sockets;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Expresso
{
    public abstract class MarketPricePublisherBase
    {
        public String Name { get; }
        public string Topic { get; }

        private string _publisherEndpoint;
        private CancellationToken _cancel;
        private ConfiguredTaskAwaitable _workProc;

        public MarketPricePublisherBase(String name, String topic, String publisherEndpoint, CancellationToken token)
        {
            Name = name;
            Topic = topic;

            _publisherEndpoint = publisherEndpoint;
            _cancel = token;
            _workProc = Task.Run(Work, _cancel).ConfigureAwait(false);
        }

        public abstract Price Next();

        private void Work()
        {
            using (var publisherSocket = new PublisherSocket())
            {
                publisherSocket.Bind(_publisherEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    //let the publisher time to connect...
                    Thread.Sleep(750);

                    var price = Next();

                    if (null == price) return;

                    publisherSocket
                        .SendMoreFrame(price.Asset)
                        .SendFrame(price.Serialize());

                   
                }
            }
        }
    }
}
