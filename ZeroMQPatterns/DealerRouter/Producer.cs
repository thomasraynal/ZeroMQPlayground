using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.DealerRouter
{
    public class Producer
    {
        private string _routerEndpoint;
        private PushSocket _producer;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;

        public Producer(string routerEndpoint, CancellationToken cancel)
        {
            _routerEndpoint = routerEndpoint;
            _cancel = cancel;

            Produced = new List<Message>();

            _proc = Task.Run(Start).ConfigureAwait(false);
        }

        public List<Message> Produced { get; set; }

        public void Start()
        {

            using (_producer = new PushSocket())
            {
                _producer.Connect(_routerEndpoint);

                while (!_cancel.IsCancellationRequested)
                {

                    var message = new Message()
                    {
                        Id = Guid.NewGuid(),
                        Payload = DateTime.Now.Ticks
                    };

                    Produced.Add(message);

                    var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                    _producer.SendFrame(messageBytes);

                    Thread.Sleep(100);
                }
            }

        }

    }
}
