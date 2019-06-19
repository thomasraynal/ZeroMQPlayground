using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using NetMQ;
using NetMQ.Sockets;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class DynamicCache<TKey, TAggregate> : IDynamicCache<TKey, TAggregate>
        where TAggregate : IAggregate<TKey>, new()
    {
        private DynamicCacheConfiguration _configuration;
        private CancellationTokenSource _cancel;
        private IDisposable _stateObservable;
        private Thread _workProc;
        private Thread _heartBeatProc;

        private BehaviorSubject<DynamicCacheState> _state;
        private SubscriberSocket _cacheUpdateSocket;

        private SourceCache<TAggregate, TKey> _sourceCache { get; }

        public DynamicCache(DynamicCacheConfiguration configuration)
        {
            Id = Guid.NewGuid();

            _configuration = configuration;
            _sourceCache = new SourceCache<TAggregate, TKey>(selector => selector.Id);
            _cancel = new CancellationTokenSource();
            _state = new BehaviorSubject<DynamicCacheState>(DynamicCacheState.Disconnected);

        }

        public IObservable<DynamicCacheState> State
        {
            get
            {
                return _state.AsObservable();
            }
        }

        //todo: threadsafe?
        public IEnumerable<TAggregate> Items => _sourceCache.Items;

        public Guid Id { get; }

        public bool IsStarted { get; private set; }

        private Task GetStateOfTheWorld()
        {
            //req resp?
            using (var dealer = new DealerSocket())
            {
                var request = new StateRequest()
                {
                    Subject = _configuration.Subject,
                };

                dealer.Connect(_configuration.StateOfTheWorldEndpoint);
                dealer.SendFrame(request.Serialize());

                //parameterized
                var hasResponse = dealer.TryReceiveFrameBytes(_configuration.HeartbeatTimeout, out var responseBytes);

                if (!hasResponse) throw new Exception("unable to reach broker");

                //handle proper deserialization....
                var stateOfTheWorld = responseBytes.Deserialize<StateReply>();

                foreach (var message in stateOfTheWorld.Events)
                {
                    var @event = (IEvent<TKey, TAggregate>)message.MessageBytes.Deserialize(message.MessageType);

                    OnEventReceived(@event);
                }
            }

            return Task.CompletedTask;
        }

        private void HandleHeartbeat()
        {
            while (!_cancel.IsCancellationRequested)
            {
                using (var heartbeat = new RequestSocket(_configuration.HearbeatEndpoint))
                {
                    heartbeat.SendFrame(Heartbeat.Query.Serialize());
                    var response = heartbeat.TryReceiveFrameBytes(_configuration.HeartbeatDelay, out var responseBytes);

                    if (_cancel.IsCancellationRequested) return;

                    var currentState = response ? DynamicCacheState.Connected : DynamicCacheState.Disconnected;

                    switch (currentState)
                    {
                        //raise only if previous state is disconnected
                        case DynamicCacheState.Connected:
                            if(_state.Value == DynamicCacheState.Disconnected)
                                _state.OnNext(currentState);
                            break;
                        //raise only if previous state is connected or staled
                        case DynamicCacheState.Disconnected:
                            if (_state.Value == DynamicCacheState.Connected || _state.Value == DynamicCacheState.Staled)
                                _state.OnNext(currentState);
                            break;
                    }

                }

                Thread.Sleep(_configuration.HeartbeatDelay.Milliseconds);

            }
        }

        private void HandleWork()
        {
            using (_cacheUpdateSocket = new SubscriberSocket())
            {
               
                _cacheUpdateSocket.Options.ReceiveHighWatermark = _configuration.ZmqHighWatermark;

                _cacheUpdateSocket.Subscribe(_configuration.Subject);
                _cacheUpdateSocket.Connect(_configuration.SubscriptionEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    var hasMessage = _cacheUpdateSocket.TryReceive(_configuration.IsStaleTimeout, out IEvent<TKey, TAggregate> @event);

                    if (_cancel.IsCancellationRequested) return;

                    if (hasMessage)
                    {
                        OnEventReceived(@event);
                    }
                    else
                    {
                        _state.OnNext(DynamicCacheState.Staled);
                    }
                }
            }
        }

        private void OnEventReceived(IEvent<TKey, TAggregate> @event)
        {
            var aggregate = _sourceCache.Lookup(@event.AggregateId);

            if (!aggregate.HasValue)
            {
                var @new = new TAggregate
                {
                    Id = @event.AggregateId
                };

                @new.Apply(@event);

                _sourceCache.AddOrUpdate(@new);
            }
            else
            {
                aggregate.Value.Apply(@event);

                _sourceCache.AddOrUpdate(aggregate.Value);
            }
        }

        public IObservableCache<TAggregate, TKey> AsObservableCache()
        {
            return _sourceCache.AsObservableCache();
        }

        public async Task Start()
        {
            if (IsStarted) throw new InvalidOperationException($"{nameof(DynamicCache<TKey, TAggregate>)} is already started");

            IsStarted = true;

            //todo: handle reconnect and cache recreate
            _stateObservable = _state.Subscribe((state) =>
             {

             });

            _workProc = new Thread(HandleWork)
            {
                IsBackground = true
            };

            _workProc.Start();

            _heartBeatProc = new Thread(HandleHeartbeat)
            {
                IsBackground = true
            };

            _heartBeatProc.Start();

            await GetStateOfTheWorld();
        }

        public Task Stop()
        {
            _cancel.Cancel();

            _stateObservable.Dispose();

            _state.OnNext(DynamicCacheState.Disposed);
            _state.OnCompleted();

            _sourceCache.Dispose();

            _cacheUpdateSocket.Close();
            _cacheUpdateSocket.Dispose();

            IsStarted = false;

            return Task.CompletedTask;
        }
    }
}
