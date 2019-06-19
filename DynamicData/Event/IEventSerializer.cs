namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface IEventSerializer
    {
        string GetAggregateId(string subject);
        string GetSubject<TKey, TAggregate>(IEvent<TKey, TAggregate> @event) where TAggregate : IAggregate<TKey>;
        IEvent<TKey, TAggregate> ToEvent<TKey, TAggregate>(TransportMessage transportMessage) where TAggregate : IAggregate<TKey>;
        TransportMessage ToTransportMessage<TKey, TAggregate>(IEvent<TKey, TAggregate> @event) where TAggregate : IAggregate<TKey>;
    }
}