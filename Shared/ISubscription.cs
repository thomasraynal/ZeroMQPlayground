using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface ISubscription
    {
        Func<IEvent, bool> CanHandle { get; set; }
    }

    public interface ISubscription<TEvent> : ISubscription where  TEvent : IEvent
    {
      
    }
}
