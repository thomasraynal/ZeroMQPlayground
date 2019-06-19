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
    public class DynamicCache<TKey, TAggregate> : IDynamicCache<TKey, TAggregate>, IDisposable
        where TAggregate : IAggregate<TKey>, new()
    {
        private DynamicCacheConfiguration _configuration;
        private CancellationTokenSource _cancel;
        private Thread _workProc;

        private BehaviorSubject<bool> _isConnected;
        private BehaviorSubject<bool> _isCaughtUp;
        private BehaviorSubject<bool> _isStale;

        private Thread _heartBeatProc;

        private SourceCache<TAggregate, TKey> _sourceCache { get; }

        public DynamicCache()
        {
            _sourceCache = new SourceCache<TAggregate, TKey>(selector => selector.Id);
        }

        public IObservable<bool> IsConnected
        {
            get
            {
                return _isConnected.AsObservable();
            }
        }

        public IObservable<bool> IsCaughtUp
        {
            get
            {
                return _isCaughtUp.AsObservable();
            }
        }

        public IObservable<bool> IsStale
        {
            get
            {
                return _isStale.AsObservable();
            }
        }

        //todo: threadsafe?
        public IEnumerable<TAggregate> Items => _sourceCache.Items;

        public async Task Connect(DynamicCacheConfiguration configuration)
        {

            _configuration = configuration;

            _cancel = new CancellationTokenSource();

            _isStale = new BehaviorSubject<bool>(true);
            _isCaughtUp = new BehaviorSubject<bool>(false);

            _workProc = new Thread(Work)
            {
                IsBackground = true
            };

            _workProc.Start();

            //todo: handle disconnect, reconnect and stale state
            _heartBeatProc = new Thread(Heartbeat)
            {
                IsBackground = true
            };

            _heartBeatProc.Start();


            await GetStateOfTheWorld();

        }

        private Task GetStateOfTheWorld()
        {
            using (var dealer = new DealerSocket())
            {
                var request = new StateRequest()
                {
                    //Topic = _configuration.Topic,
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

        private void Heartbeat()
        {

        }

        private void Work()
        {
            using (var sub = new SubscriberSocket())
            {
               
                sub.Options.ReceiveHighWatermark = _configuration.ZmqHighWatermark;

                sub.Subscribe(_configuration.Subject);
                sub.Connect(_configuration.SubscriptionEndpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    //todo : define platform agnostic protocol
                    var @event = sub.Receive<IEvent<TKey, TAggregate>>();

                    OnEventReceived(@event);

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

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
