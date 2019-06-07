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

namespace ZeroMQPlayground.RouterDealer
{
    public class Worker
    {
        private string _clusterEndpoint;
        private RequestSocket _worker;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;
 

        public Worker(string clusterEndpoint, CancellationToken cancel)
        {
            _clusterEndpoint = clusterEndpoint;
            _cancel = cancel;

            WorkerId = Guid.NewGuid();

            _proc = Task.Run(Start).ConfigureAwait(false);
;
        }

        public Guid WorkerId { get; private set; }

        public void Start()
        {

            using (_worker = new RequestSocket())
            {
                _worker.Options.Identity = WorkerId.ToByteArray();
                _worker.Connect(_clusterEndpoint);

                var ready = new Work()
                {
                    Status = WorkerStatus.Ready
                };

                _worker.SendFrame(ready.Serialize());

                while (!_cancel.IsCancellationRequested)
                {

                    var workBytes = _worker.ReceiveFrameBytes();
                    var work = workBytes.Deserialize<Work>();
                    work.Status = WorkerStatus.Finished;

                    Thread.Sleep(100);

                    var messageBytes = work.Serialize();
                    _worker.SendFrame(messageBytes);
                }
            }


        }

    }
}
