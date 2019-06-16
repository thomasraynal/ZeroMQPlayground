using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public interface ICanHeartbeat
    {
        IObservable<bool> IsConnected { get; }
        void DoHeartbeat(TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay);
    }
}
