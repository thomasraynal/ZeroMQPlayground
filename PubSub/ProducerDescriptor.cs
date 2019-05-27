using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class ProducerDescriptor
    {
        public static ProducerDescriptor NoAvailableProducer = new ProducerDescriptor()
        {
            Id = Guid.Empty,
            LastActivated = DateTime.MaxValue,
            Topic = null,
        };

        public Guid Id { get; set; }
        public String Topic { get; set; }
        public String Endpoint { get; set; }

        public ProducerState State { get; set; }

        [JsonIgnore]
        public DateTime LastActivated { get; set; }
    }
}
