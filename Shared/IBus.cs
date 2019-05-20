using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.Shared
{
    public interface IBus
    {
        IContainer Container { get; }
        IPeer Self { get; }
        void Register<TEvent>(IEventHandler<TEvent> @event) where TEvent : IEvent;
        Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult;
        void Emit(IEvent @event);
        void Subscribe<TEvent>(Func<TEvent, bool> predicate) where TEvent : IEvent;
        void Subscribe<TEvent>() where TEvent : IEvent;
    }
}
