using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.RouterRouter
{
    public class LoadBalancer
    {

        private readonly string _frontendRouterEndpoint;
        private readonly string _backendRouterEndpoint;
        private RouterSocket _backend;
        private RouterSocket _frontend;
        private readonly ConfiguredTaskAwaitable _endpointProc;
        private readonly ConfiguredTaskAwaitable _enqueueWorks;
        private Queue<byte[]> _workers;
        private BlockingCollection<Work> _works;
        private NetMQPoller _poller;

        public LoadBalancer(string frontendEndpoint, string backendEnpoint)
        {
            _frontendRouterEndpoint = frontendEndpoint;
            _backendRouterEndpoint = backendEnpoint;
      
            _workers = new Queue<byte[]>();
            _works = new BlockingCollection<Work>();

            _endpointProc = Task.Run(Start).ConfigureAwait(false);
            _enqueueWorks = Task.Run(Work).ConfigureAwait(false);
        }

        private void Work()
        {
            foreach (var work in _works.GetConsumingEnumerable())
            {
                byte[] worker;

                while (!_workers.TryDequeue(out worker))
                {
                    Task.Delay(20).Wait();
                }

                work.WorkerId = worker;

                _backend.SendMoreFrame(worker)
                        .SendMoreFrameEmpty()
                        .SendFrame(work.Serialize());
            }

        }

        public void Kill()
        {
            _poller.Stop();

            _frontend.Close();
            _frontend.Dispose();

            _backend.Close();
            _backend.Dispose();
        }

        public void Start()
        {
            using (_frontend = new RouterSocket())
            {
                _frontend.Bind(_frontendRouterEndpoint);

                using (_backend = new RouterSocket())
                {
                    _backend.Bind(_backendRouterEndpoint);

                    using ( _poller = new NetMQPoller { _frontend, _backend })
                    {

                        _frontend.ReceiveReady += (s, e) =>
                        {
                            var transportMessage = e.Socket.ReceiveMultipartMessage()
                                                           .GetMessageFromRouter<Work>();

                            var work = transportMessage.Message;

                            work.ClientId = transportMessage.SenderId;

                            if (work.Status == WorkerStatus.Ask)
                            {
                                _works.Add(work);
                            }

                        };

                        _backend.ReceiveReady += (s, e) =>
                        {
                            var transportMessage = e.Socket.ReceiveMultipartMessage()
                                                 .GetMessageFromRouter<Work>();

                            var work = transportMessage.Message;

                            if (work.Status == WorkerStatus.Ready)
                            {
                                _workers.Enqueue(transportMessage.SenderId);
                            }
                            if (work.Status == WorkerStatus.Finished)
                            {
                                _frontend.SendMoreFrame(work.ClientId)
                                         .SendMoreFrameEmpty()
                                         .SendFrame(transportMessage.MessageBytes);

                                _workers.Enqueue(transportMessage.SenderId);
                            }

                        };

                        _poller.Run();

                    }
                }
            }

        }
    }
}
