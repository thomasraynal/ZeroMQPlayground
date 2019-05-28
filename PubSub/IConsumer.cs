using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public interface IConsumer
    {
        void Start();
        void Stop();
    }


    public interface IConsumer<TEvent> : IConsumer
    {
        IObservable<TEvent> GetSubscription();
    }
}
