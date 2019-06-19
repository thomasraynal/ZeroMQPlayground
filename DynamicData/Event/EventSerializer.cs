using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{

    public class EventSerializer : IEventSerializer
    {
        public const string All = "*";
        public const string Separator = ".";
        private ISerializer _serializer;

        public EventSerializer(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public string GetAggregateId(string subject)
        {
            return subject.Split(Separator).First();
        }

        //todo review event suject setting and versioning

        public IEvent<TKey, TAggregate> ToEvent<TKey, TAggregate>(TransportMessage transportMessage) where TAggregate : IAggregate<TKey>
        {
            var @event = (IEvent<TKey, TAggregate>)_serializer.Deserialize(transportMessage.MessageBytes,transportMessage.MessageType);
            @event.Version = transportMessage.Version;
            return @event;
        }

        public TransportMessage ToTransportMessage<TKey, TAggregate>(IEvent<TKey, TAggregate> @event) where TAggregate : IAggregate<TKey>
        {
            @event.Subject = GetSubject(@event);

            var message = new TransportMessage()
            {
                MessageBytes = _serializer.Serialize(@event),
                Subject = @event.Subject,
                MessageType = @event.GetType(),
                Version = @event.Version,
            };

            return message;

        }

        public string GetSubject<TKey, TAggregate>(IEvent<TKey, TAggregate> @event) where TAggregate : IAggregate<TKey>
        {
            var tokens = GetTokens(@event.GetType());

            var subject = tokens.Select(token => @event.GetType().GetProperty(token.PropertyInfo.Name).GetValue(@event, null))
                         .Select(obj => null == obj ? All : obj.ToString())
                         .Aggregate((token1, token2) => $"{token1}{Separator}{token2}");

            return $"{@event.AggregateId}.{subject}";

        }

        private IEnumerable<PropertyToken> GetTokens(Type messageType)
        {

            var properties = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Select(prop => new { attributes = prop.GetCustomAttributes(typeof(RoutingPositionAttribute), true), property = prop })
                                    .Where(selection => selection.attributes.Count() > 0)
                                    .Select(selection => new PropertyToken(((RoutingPositionAttribute)selection.attributes[0]).Position, messageType, selection.property));

            return properties.OrderBy(x => x.Position).ToArray();
        }
    }
}
