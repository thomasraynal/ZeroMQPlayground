using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Transport : ITransport, IDisposable
    {
        private readonly IBus _bus;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IMessageDispatcher _messageDispatcher;
 
        public Transport(IBus bus)
        {
            _bus = bus;
            _cancellationTokenSource = new CancellationTokenSource();
            _messageDispatcher = _bus.Container.GetInstance<IMessageDispatcher>();

            Task.Run(Pull, _cancellationTokenSource.Token);

        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Pull()
        {
            using (var receiver = new PullSocket(_bus.Self.Endpoint))
            {
                while (true)
                {

                    var bytes = receiver.ReceiveFrameBytes();
                    var message = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(bytes));

                    _messageDispatcher.Dispatch(message);

                }
            }
        }

        public void Send(IEvent @event)
        {
            throw new NotImplementedException();
        }
    }
}
