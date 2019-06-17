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
        private CancellationToken _cancel;
        private string _endpoint;
        private Thread _workProc;

        private BehaviorSubject<bool> _connectionStatus;
        private BehaviorSubject<bool> _isCaughtUp;
        private BehaviorSubject<bool> _isStale;

        private SourceCache<TAggregate, TKey> _sourceCache { get; }

        public DynamicCache()
        {
            _sourceCache = new SourceCache<TAggregate, TKey>(selector => selector.Id);
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

        public Task Connect(string endpoint, CancellationToken cancel)
        {
            _cancel = cancel;
            _endpoint = endpoint;

            _isStale = new BehaviorSubject<bool>(true);
            _isCaughtUp = new BehaviorSubject<bool>(false);

            _workProc = new Thread(Work);
            _workProc.Start();

            return Task.CompletedTask;
        }

        private void Work()
        {
            using (var sub = new SubscriberSocket())
            {
                //todo : handle subcription filter via lambda
                sub.SubscribeToAnyTopic();
                sub.Connect(_endpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    //todo : define platform agnostic protocol
                    var @event = sub.Receive<IEvent<TKey, TAggregate>>();

                    var aggregate = _sourceCache.Lookup(@event.Message.AggregateId);

                    if (!aggregate.HasValue)
                    {
                        var @new = new TAggregate();
                        @new.Id = @event.Message.AggregateId;
                        @new.Apply(@event.Message);

                        _sourceCache.AddOrUpdate(@new);
                    }
                    else
                    {
                        aggregate.Value.Apply(@event.Message);
                    }

                }
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
