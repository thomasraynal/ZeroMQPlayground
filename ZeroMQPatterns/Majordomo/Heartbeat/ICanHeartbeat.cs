using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public interface ICanHeartbeat
    {
        IObservable<bool> IsConnected { get; }
        void DoHeartbeat(string[] targets, TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay);
    }
}
