using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
//https://stackoverflow.com/questions/5780888/casting-interfaces-for-deserialization-in-json-net
namespace ZeroMQPlayground.PushPull
{
    public class AbstractConverter<TReal, TAbstract> : JsonConverter where TReal : TAbstract
    {
        public override Boolean CanConvert(Type objectType)
            => objectType == typeof(TAbstract);

        public override Object ReadJson(JsonReader reader, Type type, Object value, JsonSerializer jser)
            => jser.Deserialize<TReal>(reader);

        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer jser)
            => jser.Serialize(writer, value);
    }
}
