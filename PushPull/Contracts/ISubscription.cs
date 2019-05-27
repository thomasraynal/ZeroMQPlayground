using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ZeroMQPlayground.PushPull
{
    public interface ISubscription
    {
        bool CanHandle(IEvent @event);
        Func<IEvent, bool> ToFunc(Expression exp);
        Func<IEvent, bool> CanHandleFunc { get; set; }
        Type EventType { get; set; }
        String CanHandleExpression { get; set; }
    }

    public interface ISubscription<TEvent> : ISubscription where TEvent : class, IEvent
    {
    }
}
