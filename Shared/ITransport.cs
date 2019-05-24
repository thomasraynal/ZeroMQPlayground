using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.Shared
{
    public interface ITransport
    {
        void Send(IEvent @event);
        void Send(ICommandResult @event, IPeer peer);
        Task Start();
        void Stop();
        Task<ICommandResult> Send(ICommand command, IPeer peer);
    }
}
