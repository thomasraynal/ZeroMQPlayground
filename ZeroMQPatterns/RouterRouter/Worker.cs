using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.RouterRouter
{
    public class Worker
    {
        private string _loadBalancerEndpoint;
        private RequestSocket _worker;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;
        private readonly Guid _id;

        public Worker(string loadBalancerEndpoint, CancellationToken cancel)
        {
            _loadBalancerEndpoint = loadBalancerEndpoint;
            _cancel = cancel;
            _proc = Task.Run(Start).ConfigureAwait(false);
            _id = Guid.NewGuid();
        }

        public void Start()
        {

            using (_worker = new RequestSocket())
            {
                _worker.Connect(_loadBalancerEndpoint);

                var ready = new Work()
                {
                    Status = WorkerStatus.Ready
                };

                var readyBytes = ready.Serialize();

                _worker.SendFrame(readyBytes);

                while (!_cancel.IsCancellationRequested)
                {

                    var workBytes = _worker.ReceiveFrameBytes();
                    var work = JsonConvert.DeserializeObject<Work>(Encoding.UTF8.GetString(workBytes));
                    work.Status = WorkerStatus.Finished;
                    Task.Delay(100).Wait();

                    var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(work));
                    _worker.SendFrame(messageBytes);
                }
            }


        }

    }
}
