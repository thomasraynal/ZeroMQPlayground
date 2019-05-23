using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Bus : IBus
    {

        private readonly List<IEventHandler> _handlers;
        private ITransport _transport;

        public IPeer Self { get; private set; }
        public IContainer Container { get; private set; }

        public Bus(IContainer container, IPeer self)
        {
            _handlers = new List<IEventHandler>();

            container.Configure(conf => conf.For<IPeer>().Use(self));

            Container = container;
            Self = self;
        }

        public void Register<TEvent>(IEventHandler<TEvent> @event) where TEvent : IEvent
        {
            _handlers.Add(@event);
        }

        public void Emit(IEvent @event)
        {
            _transport.Send(@event);
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : class, ICommandResult
        {
            return _transport.Send(command).ContinueWith(result => result.Result as TCommandResult);
        }

        public void Subscribe<TEvent>(Expression<Func<TEvent, bool>> predicate) where TEvent : class, IEvent
        {
            Self.Subscriptions.Add(new Subscription<TEvent>(predicate));

            _transport.Send(new PeerUpdatedEvent() { Peer = Self });
        }

        public void Subscribe<TEvent>() where TEvent : class, IEvent
        {
            Self.Subscriptions.Add(new Subscription<TEvent>());

            _transport.Send(new PeerUpdatedEvent() { Peer = Self });
        }

        public IEnumerable<IEventHandler> GetHandlers(Type message)
        {
            return _handlers.Where(handler => handler.GetType().GetGenericArguments().Any(arg => arg == message));
        }

        public void Start()
        {
            _transport = Container.GetInstance<ITransport>();

            var directory = Container.GetInstance<IDirectory>();

            _handlers.Add(directory);

            Task.Delay(200).Wait();

            Self.Subscriptions.Add(new Subscription<PeerUpdatedEvent>());

        }
    }
}
