using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class Market
    {

        private static readonly string[] CcyPairs = { "EUR/USD", "EUR/JPY", "EUR/GBP", "EUR/CDN" };

        private string _routerEndpoint;
        private CancellationToken _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private readonly Random _rand = new Random();

        public Market(String routerEndpoint, CancellationToken token)
        {
            _routerEndpoint = routerEndpoint;
            _cancel = token;
            _workProc = Task.Run(Work, _cancel).ConfigureAwait(false);
        }

        private ChangeStockPrice Next()
        {
            var mid = _rand.NextDouble() * 10;
            var spread = _rand.NextDouble() * 2;

            var topic = CcyPairs[_rand.Next(0, 3)];

            var price = new ChangeStockPrice(
                ask: mid + spread,
                bid: mid - spread,
                mid: mid,
                spread: spread,
                stockId: topic
            );

            return price;
        }

        private void Work()
        {
            using (var publisherSocket = new PublisherSocket())
            {
                publisherSocket.Connect(_routerEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    //let the publisher time to connect...
                    Thread.Sleep(750);

                    var changePrice = Next();

                    publisherSocket
                        .SendMoreFrame(changePrice.StockId)
                        .SendFrame(changePrice.Serialize());

                }
            }
        }
    }
}
