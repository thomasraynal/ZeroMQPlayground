using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.ParanoidPirate
{
    public class Client
    {
        private readonly string _gatewayEndpoint;
        private readonly string _gatewayHeartbeatEndpoint;
        private Guid _id;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;
        private readonly BehaviorSubject<bool> _isConnected;

        public Client(string gatewayEndpoint, string gatewayHeartbeatEndpoint, CancellationToken cancel)
        {
            _gatewayEndpoint = gatewayEndpoint;
            _gatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            _cancel = cancel;

            _id = Guid.NewGuid();

            _isConnected = new BehaviorSubject<bool>(false);

            _proc = Task.Run(HeartBeat).ConfigureAwait(false);

        }

        public IObservable<bool> IsConnected
        {
            get
            {
                return _isConnected.AsObservable();
            }
        }

        public void HeartBeat()
        {
            while (!_cancel.IsCancellationRequested)
            {
                using (var heartbeat = new RequestSocket(_gatewayHeartbeatEndpoint))
                {
                    heartbeat.SendFrame(Heartbeat.Query.Serialize());

                    var response = heartbeat.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(500), out var responseBytes);

                    _isConnected.OnNext(response);
                }

                Thread.Sleep(500);
            }

            _isConnected.OnCompleted();

        }
        

        public Task<Work> DoWork()
        {
            if (!_isConnected.Value) throw new Exception("lost connection to gateway");

            using (var client = new RequestSocket())
            {
                client.Options.Identity = _id.ToByteArray();
                client.Connect(_gatewayEndpoint);

                client.SendFrame(Work.Ask.Serialize());

                if (client.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(1000), out var responseBytes))
                {
                    var response = responseBytes.Deserialize<Work>();
                    return Task.FromResult(response);
                }

            }

            throw new Exception("something wrong happened");

        }

    }
}
