using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.Shared
{
    public interface ITransport
    {
        void Send(IEvent @event);
        Task<ICommandResult> Send(ICommand command);
    }
}
