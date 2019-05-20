using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface ITransport
    {
        void Send(IEvent @event);
    }
}
