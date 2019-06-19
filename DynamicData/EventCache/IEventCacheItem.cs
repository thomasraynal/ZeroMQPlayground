using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public interface IEventCacheItem
    {
        byte[] Message { get; set; }
        IEventId EventId { get; set; }
    }
}