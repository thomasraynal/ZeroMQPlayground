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

        private readonly HashSet<IEventHandler> _handlers;
        private ITransport _transport;

        public IPeer Self { get; private set; }
        public IPeer PeerDirectory { get; private set; }
        public IContainer Container { get; private set; }

        public Bus(IContainer container, IPeer self, IPeer peerDirectory)
        {
            _handlers = new HashSet<IEventHandler>();

            container.Configure(conf => conf.For<IPeer>().Use(self));

            PeerDirectory = peerDirectory;

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

        public void Send(ICommandResult command, IPeer peer)
        {
            _transport.Send(command, peer);
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command, IPeer peer) where TCommandResult : class, ICommandResult
        {
            return _transport.Send(command, peer).ContinueWith(result => result.Result as TCommandResult);
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
            return _handlers.Where(handler =>
            {
                var type = typeof(IEventHandler<>).MakeGenericType(message);
                return handler.GetType().GetInterfaces().Any(@interface => @interface == type);
            });
        }

        public  void Start()
        {
            _transport = Container.GetInstance<ITransport>();

            var directory = Container.GetInstance<IDirectory>();

            _handlers.Add(directory);

             Task.Delay(200).Wait();

            Self.Subscriptions.Add(new Subscription<PeerUpdatedEvent>());
            Self.Subscriptions.Add(new Subscription<PeerRegisterCommandResult>());

            _transport.Start().Wait();

        }

        public void Stop()
        {
            _transport.Stop();
        }
    }
}
