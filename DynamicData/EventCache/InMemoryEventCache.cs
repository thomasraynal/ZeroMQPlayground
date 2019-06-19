using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public class InMemoryEventCache : IEventCache
    {

        private readonly IEventIdProvider _eventIdProvider;
        private readonly IEventSerializer _eventSerializer;

        private readonly ConcurrentDictionary<string, SortedList<long,IEventCacheItem>> _cache;

        public InMemoryEventCache(IEventIdProvider eventIdProvider, IEventSerializer eventSerializer)
        {
            _cache = new ConcurrentDictionary<string, SortedList<long, IEventCacheItem>>();
            _eventIdProvider = eventIdProvider;
            _eventSerializer = eventSerializer;
        }

        public Task<IEventId> AppendToStream(string subject, byte[] payload)
        {
            var streamId = _eventSerializer.GetAggregateId(subject);
            var eventID = _eventIdProvider.Next(streamId, subject);

            var stream = _cache.GetOrAdd(streamId, (_) =>
             {
                 return new SortedList<long, IEventCacheItem>();
             });

            stream.Add(eventID.Version, new EventCacheItem()
            {
                Message = payload,
                EventId = eventID
            });

            return Task.FromResult(eventID);

        }

        public Task<IEnumerable<IEventCacheItem>> GetStream(string streamId)
        {
            if (!_cache.ContainsKey(streamId)) return Task.FromResult(Enumerable.Empty<IEventCacheItem>());

            return Task.FromResult(_cache[streamId].Values.AsEnumerable());
        }

        public Task<IEnumerable<IEventCacheItem>> GetStreamBySubject(string subject)
        {
            var streamId = _eventSerializer.GetAggregateId(subject);

            if (!_cache.ContainsKey(streamId)) return Task.FromResult(Enumerable.Empty<IEventCacheItem>());

            return Task.FromResult(_cache[streamId].Values.Where(ev => ev.EventId.Subject == subject).AsEnumerable());
        }
    }
}
