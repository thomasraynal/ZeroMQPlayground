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
    public class Consumer
    {
        private string _dealerEndpoint;
        private PullSocket _puller;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;

        public Consumer(string dealerEndpoint, CancellationToken cancel)
        {
            _dealerEndpoint = dealerEndpoint;
            _cancel = cancel;

            Received = new List<Message>();

            _proc = Task.Run(Start).ConfigureAwait(false);

        }

        public List<Message> Received { get; set; }

        public void Start()
        {
            using (_puller = new PullSocket())
            {
                _puller.Connect(_dealerEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    var messageBytes = _puller.ReceiveFrameBytes();

                    var message = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(messageBytes));

                    Received.Add(message);
                }
            }
        }
    }
}
