using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class Client<TDto> : ICanHeartbeat where TDto : ISubjectDto
    {
        private readonly ClientConfiguration _configuration;
        private BrokerDescriptor _currentBroker;
       
        private ConfiguredTaskAwaitable _heartbeatProc;
        private ConfiguredTaskAwaitable _workProc;
        private IDisposable _disconnected;
        private DealerSocket _getMarketState;
        private SubscriberSocket _getMarketUpdates;
        private PublisherSocket _publishMarketUpdates;

        private readonly BehaviorSubject<bool> _isConnected;

        private CancellationTokenSource _cancel;
        private CancellationTokenSource _cancelUpdatePublisher;

        private ManualResetEventSlim _resetEvent = new ManualResetEventSlim(false);

        public SortedList<long, ISequenceItem<TDto>> Updates { get; set; }
        public Guid Id { get; }
        public String Name { get; }

        public BrokerDescriptor Broker => _currentBroker;

        public IObservable<bool> IsConnected
        {
            get
            {
                return _isConnected.AsObservable();
            }
        }

        public Client(ClientConfiguration configuration)
        {
            _configuration = configuration;
            _cancel = new CancellationTokenSource();
            _cancelUpdatePublisher = new CancellationTokenSource();

            Updates = new SortedList<long, ISequenceItem<TDto>>();

            Id = Guid.NewGuid();
            Name = _configuration.Name;

            _isConnected = new BehaviorSubject<bool>(false);


        }
        public void Push(ISubjectDto dto)
        {
            if (!_isConnected.Value) throw new InvalidOperationException("not connected");

            _publishMarketUpdates
                .SendMoreFrame(dto.Subject.Serialize())
                .SendFrame(dto.Serialize());
        }

        public async Task Start()
        {
            _publishMarketUpdates = new PublisherSocket();
  
            foreach (var broker in _configuration.BrokerDescriptors)
            {
                _publishMarketUpdates.Connect(broker.PublishUpdateEndpoint);
            }

            _disconnected = IsConnected
                .Buffer(2, 1)
                .Where(buffer => buffer.Count > 1 && buffer[0] && !buffer[1])
                .Subscribe(async _ =>
                {
                    await DoStart();
                });

            await DoStart();
        }

        private async Task DoStart()
        {
            await TryGetBroker();

            await Cleanup();

            await ConnectToBroker();
        }

        private Task TryGetBroker()
        {
            _currentBroker = null;

            var doesHeartbeat = false;

            while (!doesHeartbeat)
            {
                var broker = _configuration.Next();

                doesHeartbeat = DoHeartbeatInternal(broker.BrokerHeartbeatEndpoint, _configuration.HearbeatMaxDelay);

                if (doesHeartbeat)
                {
                    _currentBroker = broker;
                    _isConnected.OnNext(doesHeartbeat);
                    return Task.CompletedTask;
                }

            }

            return Task.CompletedTask;
        }

        private Task Cleanup()
        {
            _cancel.Cancel();

            if (null != _getMarketState)
            {
                _getMarketState.Close();
                _getMarketState.Dispose();
            }

            if (null != _getMarketUpdates)
            {
                _getMarketUpdates.Close();
                _getMarketUpdates.Dispose();
            }

            return Task.CompletedTask;
        }

        private async Task ConnectToBroker()
        {
            _cancel = new CancellationTokenSource();

            _getMarketState = new DealerSocket();
            _getMarketState.Connect(_currentBroker.GetStateEndpoint);
            _getMarketState.Options.Identity = Id.ToByteArray();

            _getMarketUpdates = new SubscriberSocket();
            _getMarketUpdates.SubscribeToAnyTopic();
            _getMarketUpdates.Connect(_currentBroker.SubscribeToUpdatesEndpoint);

            _heartbeatProc = Task.Run(() => DoHeartbeat(_configuration.HearbeatDelay, _configuration.HearbeatMaxDelay), _cancel.Token).ConfigureAwait(false);
            _workProc = Task.Run(ReceiveUpdates, _cancel.Token).ConfigureAwait(false);

            await Synchronize();

        }

        private void ReceiveUpdates()
        {
            _resetEvent.Wait();

            while (!_cancel.IsCancellationRequested)
            {
                var enveloppe = _getMarketUpdates.ReceiveMultipartMessage()
                                                 .GetMessageFromPublisher<ISequenceItem<TDto>>();

                Updates.Add(enveloppe.Message.Position, enveloppe.Message);

            }
        }

        private Task Synchronize()
        {
            _getMarketState.SendFrame(StateRequest.Default.Serialize());

            var enveloppe = _getMarketState.ReceiveMultipartMessage()
                                           .GetMessageFromPublisher<StateReply<TDto>>();

            Updates.Clear();

            foreach (var update in enveloppe.Message.Updates)
            {
                Updates.Add(update.Position, update);
            }

            _resetEvent.Set();

            return Task.CompletedTask;
        }

        public async Task Stop()
        {

            _isConnected.OnCompleted();
            _disconnected.Dispose();

            _publishMarketUpdates.Close();
            _publishMarketUpdates.Dispose();

            await Cleanup();
        }

        public void DoHeartbeat(TimeSpan hearbeatDelay, TimeSpan hearbeatMaxDelay)
        {
            while (!_cancel.IsCancellationRequested)
            {
                var response = DoHeartbeatInternal(_currentBroker.BrokerHeartbeatEndpoint, hearbeatMaxDelay);

                _isConnected.OnNext(response);

                Thread.Sleep(hearbeatDelay.Milliseconds);
            }
        }

        private bool DoHeartbeatInternal(string target, TimeSpan hearbeatMaxDelay)
        {
            using (var heartbeat = new RequestSocket(target))
            {
                heartbeat.SendFrame(Heartbeat.Query.Serialize());

                return heartbeat.TryReceiveFrameBytes(hearbeatMaxDelay, out var responseBytes);
            }
        }
    }  
}
