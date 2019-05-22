using StructureMap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class MessageHandlerInvokerCache
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

        private readonly ConcurrentDictionary<MessageHandlerInvokerCacheKey, MethodInfo> _methodInfoCache;
        private readonly ConcurrentDictionary<Type, IEnumerable<IEventHandler>> _handlerCache;
        private readonly IContainer _container;

        public MessageHandlerInvokerCache(IContainer container)
        {
            _methodInfoCache = new ConcurrentDictionary<MessageHandlerInvokerCacheKey, MethodInfo>();
            _handlerCache = new ConcurrentDictionary<Type, IEnumerable<IEventHandler>>();
            _container = container;
        }

        public IEnumerable<IEventHandler> GetHandlers(Type messageHandlerType)
        {
            return _handlerCache.GetOrAdd(messageHandlerType, _container.GetAllInstances(messageHandlerType).Cast<IEventHandler>());
        }

        public MethodInfo GetMethodInfo(Type handlerType, Type messageHandlerType)
        {
            var key = new MessageHandlerInvokerCacheKey(handlerType, messageHandlerType);

            return _methodInfoCache.GetOrAdd(key, messageHandlerType.GetMethod("Handle", new[] { handlerType }));
        }
    }
}
