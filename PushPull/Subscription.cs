using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;
using Serialize.Linq.Extensions;
using Serialize.Linq.Serializers;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Serialization = Serialize.Linq.Serializers;

namespace ZeroMQPlayground.PushPull
{
    public class Subscription<TEvent> : ISubscription<TEvent> where TEvent : class, IEvent
    {
        public Subscription()
        {
            var expressionSerializer = new ExpressionSerializer(new Serialization.JsonSerializer());

            Expression<Func<TEvent, bool>> exp = (ev) => ev.GetType() == typeof(TEvent);

            CanHandleExpression = expressionSerializer.SerializeText(exp);

            CanHandleFunc = (ev) => ev.GetType() == typeof(TEvent);

            EventType = typeof(TEvent);
        }

        public Subscription(Expression<Func<TEvent, bool>> canHandle)
        {
            var expressionSerializer = new ExpressionSerializer(new Serialization.JsonSerializer());

            var func = canHandle.Compile();

            CanHandleExpression = expressionSerializer.SerializeText(canHandle);

            CanHandleFunc = (ev) => func(ev as TEvent);

            EventType = typeof(TEvent);
        }

        public String CanHandleExpression { get; set; }
        public Func<IEvent, bool> CanHandleFunc { get; set; }
        public Type EventType { get; set; }

        public bool CanHandle(IEvent @event)
        {
            return @event.GetType() == EventType && CanHandleFunc(@event);
        }

        public Func<IEvent, bool> ToFunc(Expression exp)
        {
            var typedFunc = ((Expression<Func<TEvent, bool>>)exp).Compile();
            return (ev) => (ev is TEvent && typedFunc(ev as TEvent));
        }
    }
}
