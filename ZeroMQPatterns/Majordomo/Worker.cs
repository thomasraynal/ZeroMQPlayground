using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public class Worker
    {
        private readonly string _gatewayEndpoint;
        private readonly string _gatewayHeartbeatEndpoint;

        private readonly CancellationToken _cancel;
        private ConfiguredTaskAwaitable _workProc;
        private readonly ConfiguredTaskAwaitable _heartbeatProc;
        private readonly IDisposable _disconnected;
        private readonly Guid _id;

        private readonly BehaviorSubject<bool> _isConnected;
        private readonly Random _rand;
        private RequestSocket _worker;

        public Worker(string gatewayEndpoint, string gatewayHeartbeatEndpoint, CancellationToken cancel)
        {
            _gatewayEndpoint = gatewayEndpoint;
            _gatewayHeartbeatEndpoint = gatewayHeartbeatEndpoint;
            _id = Guid.NewGuid();
            _isConnected = new BehaviorSubject<bool>(false);

            _rand = new Random();
            _cancel = cancel;
         
            _heartbeatProc = Task.Run(HeartBeat, cancel).ConfigureAwait(false);

            _disconnected = IsConnected
                                .Buffer(2, 1)
                                .Where(buffer => buffer.Count > 1 && buffer[0] && !buffer[1])
                                .Subscribe(_ =>
                                {
                                    StartWorkProc();
                                });

            StartWorkProc();

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

        }

        private void StartWorkProc()
        {
            if (null != _worker)
            {
                _worker.Close();
                _worker.Dispose();
            }

            _workProc = Task.Run(Start, _cancel).ConfigureAwait(false);
        }

        public void Stop()
        {
            _isConnected.OnCompleted();
            _disconnected.Dispose();

            _worker.Close();
            _worker.Dispose();
        }

        public void Start()
        {

            using (_worker = new RequestSocket())
            {
                _worker.Options.Identity = _id.ToByteArray();
                _worker.Connect(_gatewayEndpoint);

                while (!_isConnected.Value)
                {
                    Thread.Sleep(50);
                }

                _worker.SendFrame(Work.Ready.Serialize());

                while (!_cancel.IsCancellationRequested)
                {
                    var work = _worker.ReceiveFrameBytes()
                                      .Deserialize<Work>();

                    if (_cancel.IsCancellationRequested) return;

                    work.MessageType = MessageType.Finished;

                    Thread.Sleep(_rand.Next(50, 250));

                    _worker.SendFrame(work.Serialize());
                }
            }
        }
    }
}
