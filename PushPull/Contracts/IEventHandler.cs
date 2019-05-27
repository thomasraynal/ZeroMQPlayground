using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PushPull
{
    public interface IEventHandler
    {
    }

    public interface IEventHandler<TEvent> : IEventHandler where TEvent : IEvent
    {
         void Handle(TEvent @event);
    }
}
