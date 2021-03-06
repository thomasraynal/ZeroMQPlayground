﻿using DynamicData;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.DynamicData.Dto;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class DynamicCache<TKey, TAggregate> : ActorBase, IDynamicCache<TKey, TAggregate>
        where TAggregate : IAggregate<TKey>, new()
    {
        private readonly IDynamicCacheConfiguration _configuration;
        private readonly CancellationTokenSource _cancel;

        private IDisposable _observeCacheState;

        private ConfiguredTaskAwaitable _workProc;
        private ConfiguredTaskAwaitable _heartbeatProc;

        private BehaviorSubject<DynamicCacheState> _state;
        private SubscriberSocket _cacheUpdateSocket;
        private readonly IEventSerializer _eventSerializer;

        private readonly object _caughtingUpLock = new object();
        private volatile bool _isCaughtingUp;
        private readonly CaughtingUpCache<TKey, TAggregate> _caughtingUpCache;

        private readonly SourceCache<TAggregate, TKey> _sourceCache;

        public DynamicCache(IDynamicCacheConfiguration configuration, IEventSerializer eventSerializer)
        {
   
            _eventSerializer = eventSerializer;
            _configuration = configuration;
            _sourceCache = new SourceCache<TAggregate, TKey>(selector => selector.Id);
            _cancel = new CancellationTokenSource();
            _state = new BehaviorSubject<DynamicCacheState>(DynamicCacheState.NotConnected);
            _caughtingUpCache = new CaughtingUpCache<TKey, TAggregate>();

        }

        public IObservable<DynamicCacheState> OnStateChanged()
        {
            return _state.AsObservable();
        }

        public IObservableCache<TAggregate, TKey> OnItemChanged()
        {
            return _sourceCache.AsObservableCache();
        }

        public DynamicCacheState CacheState
        {
            get
            {
                return _state.Value;
            }
        }

        public IEnumerable<TAggregate> GetItems() => _sourceCache.Items;

        private IStateReply GetStateOfTheWorld()
        {
            using (var dealer = new DealerSocket())
            {
                var request = new StateRequest()
                {
                    Subject = _configuration.Subject,
                };

                var requestBytes = _eventSerializer.Serializer.Serialize(request);

                dealer.Connect(_configuration.StateOfTheWorldEndpoint);
                dealer.SendFrame(requestBytes);

                var hasResponse = dealer.TryReceiveFrameBytes(_configuration.StateCatchupTimeout, out var responseBytes);

                if (!hasResponse) throw new Exception("unable to reach broker");

                return _eventSerializer.Serializer.Deserialize<StateReply>(responseBytes);

            }

        }

        private void HandleHeartbeat()
        {
            while (!_cancel.IsCancellationRequested)
            {
                using (var heartbeat = new RequestSocket(_configuration.HearbeatEndpoint))
                {
                    var payload = _eventSerializer.Serializer.Serialize(Heartbeat.Query); 

                    heartbeat.SendFrame(payload);

                    var response = heartbeat.TryReceiveFrameBytes(_configuration.HeartbeatTimeout, out var responseBytes);

                    if (_cancel.IsCancellationRequested) return;

                    var currentState = response ? DynamicCacheState.Connected : DynamicCacheState.Disconnected;

                    switch (currentState)
                    {
                        case DynamicCacheState.Connected:

                            if (_state.Value == DynamicCacheState.NotConnected || _state.Value == DynamicCacheState.Reconnected || _state.Value == DynamicCacheState.Staled)
                            {
                                _state.OnNext(currentState);
                            }
                            else if (_state.Value == DynamicCacheState.Disconnected)
                            {
                                _state.OnNext(DynamicCacheState.Reconnected);
                            }
                               
                            break;

                        case DynamicCacheState.Disconnected:
                            if (_state.Value == DynamicCacheState.Connected || _state.Value == DynamicCacheState.Reconnected || _state.Value == DynamicCacheState.Staled)
                            {
                                _state.OnNext(currentState);
                            }
                               
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

                    NetMQMessage message = null;

                    var hasMessage = _cacheUpdateSocket.TryReceiveMultipartMessage(_configuration.IsStaleTimeout, ref message);

                    if (_cancel.IsCancellationRequested) return;

                    if (hasMessage)
                    {
                        var eventIdBytes = message[1].Buffer;
                        var eventMessageBytes = message[2].Buffer;

                        var eventId = _eventSerializer.Serializer.Deserialize<IEventId>(eventIdBytes);
                        var producerMessage = _eventSerializer.Serializer.Deserialize<IProducerMessage>(eventMessageBytes);

                        var @event = _eventSerializer.ToEvent<TKey, TAggregate>(eventId, producerMessage);

                        OnEventReceived(@event);
                    }
                    else
                    {
                        _state.OnNext(DynamicCacheState.Staled);
                    }
                }
            }
        }

        private void ApplyEvent(IEvent<TKey, TAggregate> @event)
        {
            var aggregate = _sourceCache.Lookup(@event.EventStreamId);

            if (!aggregate.HasValue)
            {
                var @new = new TAggregate
                {
                    Id = @event.EventStreamId
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

        private void OnEventReceived(IEvent<TKey, TAggregate> @event)
        {
            //if we're trying to catchup with the event feed after a disconnect
            if (_isCaughtingUp)
            {
                //we try to acquire the lock and process the event on the catchup feed
                lock (_caughtingUpLock)
                {
                    //we got the lock - has the caughtup feed process ended meanwhile? if so, process the event on the main feed
                    if (_isCaughtingUp)
                    {
                        _caughtingUpCache.CaughtUpEvents.Add(@event);
                        return;
                    }
                }
            }

            ApplyEvent(@event);
        }

        protected override Task RunInternal()
        {

            _observeCacheState = _state
                .Where(state => state == DynamicCacheState.Reconnected)
                .Subscribe((state) =>
                 {
                     _isCaughtingUp = true;

                     _sourceCache.Edit( (updater) =>
                     {
                         updater.Clear();

                         var stateOfTheWorld = GetStateOfTheWorld();

                         var update = new Action<IEvent<TKey, TAggregate>>((@event) =>
                         {

                             var aggregate = updater.Lookup(@event.EventStreamId);

                             if (!aggregate.HasValue)
                             {
                                 var @new = new TAggregate
                                 {
                                     Id = @event.EventStreamId
                                 };

                                 @new.Apply(@event);

                                 updater.AddOrUpdate(@new);
                             }
                             else
                             {
                                 aggregate.Value.Apply(@event);

                                 updater.AddOrUpdate(aggregate.Value);
                             }

                         });

                         foreach(var eventMessage in stateOfTheWorld.Events)
                         {
                             var @event = _eventSerializer.ToEvent<TKey, TAggregate>(eventMessage);

                             update(@event);
                         }

                         lock (_caughtingUpLock)
                         {

                             var replayEvents = _caughtingUpCache.CaughtUpEvents
                                                                 .Where(ev => !stateOfTheWorld.Events.Any(msg => msg.EventId.Id == ev.EventId))
                                                                 .ToList();

                             foreach (var @event in replayEvents)
                             {
                                 update(@event);
                             }

                         }

                     });

                     _isCaughtingUp = false;

                     _caughtingUpCache.Clear();
                 });

            _workProc = Task.Run(HandleWork, _cancel.Token).ConfigureAwait(false);
            _heartbeatProc = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);

            return Task.CompletedTask;
      
        }

        protected override Task DestroyInternal()
        {
            _cancel.Cancel();

            _observeCacheState.Dispose();

            _state.OnCompleted();
            _state.Dispose();

            _sourceCache.Dispose();

            _cacheUpdateSocket.Close();
            _cacheUpdateSocket.Dispose();

            return Task.CompletedTask;
        }
    }
}
