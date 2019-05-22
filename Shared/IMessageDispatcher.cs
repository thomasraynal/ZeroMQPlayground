using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.Shared
{
    public interface IMessageDispatcher
    {
        void Dispatch(TransportMessage message);
        List<IEvent> HandledEvents { get; }
    }
}
