using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public interface IGateway : ICanHeartbeat, IHandleHeartbeat, IActor
    {
    }
}
