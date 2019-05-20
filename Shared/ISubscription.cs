using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface ISubscription
    {

    }

    public interface ISubscription<TEvent> : ISubscription where  TEvent : IEvent
    {
        Func<TEvent, bool> Subscription { get; set; }
    }
}
