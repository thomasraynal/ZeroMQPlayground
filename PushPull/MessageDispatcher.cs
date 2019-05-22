﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class MessageDispatcher : IMessageDispatcher, IDisposable
    {
        class MessageHandlerInvokerCacheKey
        {
            public MessageHandlerInvokerCacheKey(Type handlerType, Type messageHandlerType)
            {
                HandlerType = handlerType;
                MessageHandlerType = messageHandlerType;
            }

            public Type HandlerType { get; }
            public Type MessageHandlerType { get; }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }
            public override int GetHashCode()
            {
                return HandlerType.GetHashCode() ^ MessageHandlerType.GetHashCode();
            }
        }

        private readonly BlockingCollection<TransportMessage> _messageQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IBus _bus;
        private readonly ConcurrentDictionary<Type, List<MessageInvoker>> _invokers;
        private readonly MessageHandlerInvokerCache _cache;

        public List<IEvent> HandledEvents { get; set; }
   
        public MessageDispatcher(IBus bus)
        {
            _messageQueue = new BlockingCollection<TransportMessage>();
            _cancellationTokenSource = new CancellationTokenSource();
            _bus = bus;
            _cache = new MessageHandlerInvokerCache(_bus.Container);
            _invokers = new ConcurrentDictionary<Type, List<MessageInvoker>>();

            HandledEvents = new List<IEvent>();

            Task.Run(Consume, _cancellationTokenSource.Token);
        }

        private IEnumerable<MessageInvoker> GetInvokers(TransportMessage message)
        {

            var type = typeof(IEventHandler<>).MakeGenericType(message.MessageType);
            var isAsync = message.MessageType.GetCustomAttributes(true)
                                  .FirstOrDefault(attribute => attribute.GetType() == typeof(AsynchronousAttribute)) != null;

            var mode = isAsync ? DispatchMode.Asynchronous : DispatchMode.Synchronous;

            var handlers = _cache.GetHandlers(type);

            foreach (var handler in handlers)
            {
                yield return new MessageInvoker(mode, message.MessageType, handler as IEventHandler);
            }

        }

        private void Consume()
        {
            foreach (var message in _messageQueue.GetConsumingEnumerable())
            {

                var invokers = _invokers.GetOrAdd(message.MessageType, (key) =>
                {
                    return GetInvokers(message).ToList();
                });

                foreach(var invoker in invokers)
                {
                    var handlerInvoker = _cache.GetMethodInfo(message.MessageType, invoker.Handler.GetType());
                    var actualMessage = JsonConvert.DeserializeObject(Encoding.UTF32.GetString(message.Message), message.MessageType);

                    HandledEvents.Add(actualMessage as IEvent);

                    handlerInvoker.Invoke(invoker.Handler, new object[] { actualMessage });
                }
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
