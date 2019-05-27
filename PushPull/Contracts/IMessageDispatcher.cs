using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.PushPull
{
    public interface IMessageDispatcher
    {
        void Stop();
        void Dispatch(TransportMessage message);
        List<IEvent> HandledEvents { get; }
    }
}
