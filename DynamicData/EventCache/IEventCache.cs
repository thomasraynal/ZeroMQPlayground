using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroMQPlayground.DynamicData
{
    public interface IEventCache
    {
        Task<IEventId> AppendToStream(string subject, byte[] payload);
        Task<IEnumerable<IEventCacheItem>> GetStream(string streamId);
        Task<IEnumerable<IEventCacheItem>> GetStreamBySubject(string subject);
    }
}