using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public interface IWorker<TCommand,TResult> : IWorker 
        where TCommand: ICommand
        where TResult : ICommandResult
    {
        Task<TResult> Handle(TCommand command);
    }

    public interface IWorker : ICanHeartbeat, IActor
    {
    }
}
