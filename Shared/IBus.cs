using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.Shared
{
    public interface IBus
    {
        IContainer Container { get; }
        IPeer Self { get; }
        void Start();
        IEnumerable<IEventHandler> GetHandlers(Type message);
        void Register<TEvent>(IEventHandler<TEvent> @event) where TEvent : IEvent;
        Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : class, ICommandResult;
        void Emit(IEvent @event);
        void Subscribe<TEvent>(Expression<Func<TEvent, bool>> predicate) where TEvent : class, IEvent;
        void Subscribe<TEvent>() where TEvent : class, IEvent;
    }
}
