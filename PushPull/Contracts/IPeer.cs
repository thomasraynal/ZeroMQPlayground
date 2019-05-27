using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.PushPull
{
    public interface IPeer
    {
        Guid Id { get; set; }
        String Name { get; set; }
        String Endpoint { get; set; }

        [JsonProperty(ItemConverterType = typeof(SubscriptionConverter))]
        List<ISubscription> Subscriptions { get; set; }
    }
}
