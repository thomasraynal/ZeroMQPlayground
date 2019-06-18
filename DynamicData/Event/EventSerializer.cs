using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class PropertyToken
    {
        public PropertyToken(int position, Type eventType, PropertyInfo propertyInfo)
        {
            Position = position;
            EventType = eventType;
            PropertyInfo = propertyInfo;
        }

        public int Position { get; }
        public PropertyInfo PropertyInfo { get; }
        public Type EventType { get; }

    }

    public class EventSerializer
    {
        public string Serialize<TEvent>(TEvent @event)
        {
            var tokens = GetTokens(typeof(TEvent));

            return tokens.Select(token => @event.GetType().GetProperty(token.PropertyInfo.Name).GetValue(@event, null))
                         .Select(obj => null == obj ? "*" : obj.ToString())
                         .Aggregate((token1, token2) => $"{token1}.{token2}");

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
