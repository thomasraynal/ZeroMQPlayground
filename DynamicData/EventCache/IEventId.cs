namespace ZeroMQPlayground.DynamicData
{
    public interface IEventId
    {
        string EventStream { get; set; }
        string Id { get; }
        string Subject { get; set; }
        long Version { get; set; }
    }
}