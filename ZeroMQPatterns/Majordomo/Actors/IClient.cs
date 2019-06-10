using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public interface IClient : ICanHeartbeat, IActor
    {
        Task<TResult> Send<TCommand, TResult>(TCommand command, TimeSpan maxResponseDelay)
            where TResult : ICommandResult
            where TCommand : ICommand;
    }
}
