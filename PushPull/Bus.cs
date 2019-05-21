using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Bus : IBus
    {

        private readonly List<IEventHandler> _handlers;

        public IPeer Self { get; private set; }
        public IContainer Container { get; private set; }

        public Bus(IContainer container, IPeer self)
        {
            _handlers = new List<IEventHandler>();

            Container = container;
            Self = self;
        }

        public void Register<TEvent>(IEventHandler<TEvent> @event) where TEvent : IEvent
        {
            _handlers.Add(@event);
        }

        public void Emit(IEvent @event)
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult
        {
            throw new NotImplementedException();
        }

        public void Subscribe<TEvent>(Func<TEvent, bool> predicate) where TEvent : IEvent
        {
            throw new NotImplementedException();
        }

        public void Subscribe<TEvent>() where TEvent : IEvent
        {
            throw new NotImplementedException();
        }

        public IEventHandler GetHandlers(Type message)
        {
            throw new NotImplementedException();
        }
    }
}
