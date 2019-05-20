using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class MessageDispatcher : IMessageDispatcher, IDisposable
    {
        private readonly BlockingCollection<TransportMessage> _messageQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IBus _bus;

        public MessageDispatcher(IBus bus)
        {
            _messageQueue = new BlockingCollection<TransportMessage>();
            _cancellationTokenSource = new CancellationTokenSource();
            _bus = bus;

            Task.Run(Consume, _cancellationTokenSource.Token);
        }

        //private MessageHandlerInvoker GetInvoker(Type messageType)
        //{
        //    return _invokers.GetOrAdd(messageType, (key) =>
        //    {
        //        var type = typeof(IMessageHandler<>).MakeGenericType(messageType);
        //        var isAsync = messageType.GetCustomAttributes(true)
        //                              .FirstOrDefault(attribute => attribute.GetType() == typeof(AsynchronousAttribute)) != null;

        //        var mode = isAsync ? MessageHandlerInvokerMode.Asynchronous : MessageHandlerInvokerMode.Synchronous;

        //        var messageInvoker = new MessageHandlerInvoker(_cache, mode, type);

        //        return messageInvoker;
        //    });
        //}

        private void Consume()
        {
            foreach (var message in _messageQueue.GetConsumingEnumerable())
            {
                
            }
        }

        public void Dispatch(TransportMessage message)
        {
            _messageQueue.Add(message);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
