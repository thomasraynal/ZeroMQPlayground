﻿using ZeroMQPlayground.DynamicData.Dto;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface IEventSerializer
    {
        ISerializer Serializer { get; }
        string GetAggregateId(string subject);
        string GetSubject<TKey, TAggregate>(IEvent<TKey, TAggregate> @event) where TAggregate : IAggregate<TKey>;
        IEvent<TKey, TAggregate> ToEvent<TKey, TAggregate>(IEventMessage eventMessage) where TAggregate : IAggregate<TKey>;
        IEvent<TKey, TAggregate> ToEvent<TKey, TAggregate>(IEventId eventId, IProducerMessage eventMessage) where TAggregate : IAggregate<TKey>;
        IProducerMessage ToProducerMessage<TKey, TAggregate>(IEvent<TKey, TAggregate> @event) where TAggregate : IAggregate<TKey>;
    }
}