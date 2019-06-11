using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public interface IWorker<TCommand,TResult> : IWorker
    {
        Task<TResult> Handle(TCommand command);
    }

    public interface IWorker : ICanHeartbeat, IActor
    {
    }
}
