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
    //todo: ActorBase, ProducerBase
    public class Market : IActor
    {

        private static readonly string[] CcyPairs = { "EUR/USD", "EUR/GBP" };

        private string _routerEndpoint;
        private string _name;
        private CancellationTokenSource _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private readonly Random _rand = new Random();

        public Guid Id { get; }

        private IEventSerializer _eventSerializer;
        private long _generationTimespan;

        public bool IsStarted { get; private set; }

        public Market(String name, String routerEndpoint,IEventSerializer eventSerializer, long generationTimespan = 750)
        {
            Id = Guid.NewGuid();

            _eventSerializer = eventSerializer;

            _generationTimespan = generationTimespan;
            _routerEndpoint = routerEndpoint;
            _name = name;
            _cancel = new CancellationTokenSource();
         
        }
        private ChangeCcyPairPrice Next()
        {
            var mid = _rand.NextDouble() * 10;
            var spread = _rand.NextDouble() * 2;

            var topic = CcyPairs[_rand.Next(0, CcyPairs.Count())];

            var price = new ChangeCcyPairPrice(
                ask: mid + spread,
                bid: mid - spread,
                mid: mid,
                spread: spread,
                ccyPairId: topic,
                market: _name
            );

            return price;
        }

        private void HandleWork()
        {
            using (var publisherSocket = new PublisherSocket())
            {
                publisherSocket.Connect(_routerEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(_generationTimespan));

                    var changePrice = Next();

                    var message = _eventSerializer.ToProducerMessage(changePrice);

                    publisherSocket.SendMoreFrame(message.Subject)
                                   .SendFrame(_eventSerializer.Serializer.Serialize(message));

                }
            }
        }

        public Task Run()
        {
            if (IsStarted) throw new InvalidOperationException($"{nameof(Market)} is already started");

            IsStarted = true;

            _workProc = Task.Run(HandleWork, _cancel.Token).ConfigureAwait(false);

            return Task.CompletedTask;
        }

        public Task Destroy()
        {
            _cancel.Cancel();

            IsStarted = false;

            return Task.CompletedTask;
        }
    }
}
