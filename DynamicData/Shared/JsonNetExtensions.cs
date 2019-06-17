using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public static class JsonNetExtensions
    {
        public static string DeserializeString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] SerializeString(this string obj)
        {
            return Encoding.UTF8.GetBytes(obj);
        }

        public static T Deserialize<T>(this byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }

        public static object Deserialize(this byte[] bytes, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type);
        }

        public static byte[] Serialize(this object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }
    }
}
