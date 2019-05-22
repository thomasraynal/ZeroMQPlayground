using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ZeroMQPlayground.Shared;
using Serialization = Serialize.Linq.Serializers;

namespace ZeroMQPlayground.PushPull
{

    public class SubscriptionDto
    {
        public String CanHandle { get; set; }
        public Type EventType { get; set; }
    }

    public class SubscriptionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.GetInterfaces().Any(@interface => @interface == typeof(ISubscription));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var expressionSerializer = new Serialization.ExpressionSerializer(new Serialization.JsonSerializer());

            var dto = serializer.Deserialize<SubscriptionDto>(reader);
            var subType = typeof(Subscription<>).MakeGenericType(dto.EventType);
            var sub = (ISubscription)Activator.CreateInstance(subType);

            sub.EventType = dto.EventType;
            sub.CanHandleExpression = dto.CanHandle;

            var exp = expressionSerializer.DeserializeText(sub.CanHandleExpression);

            sub.CanHandleFunc = sub.ToFunc(exp);

            return sub;

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var expressionSerializer = new Serialization.ExpressionSerializer(new Serialization.JsonSerializer());
            var sub = value as ISubscription;

            var dto = new SubscriptionDto()
            {
                CanHandle = sub.CanHandleExpression,
                EventType = sub.EventType
            };

            serializer.Serialize(writer, dto);

        }
    }
}
