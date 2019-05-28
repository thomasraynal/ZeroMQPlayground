using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class ProducerRegistrationDto
    {
        public static ProducerRegistrationDto NoAvailableProducer = new ProducerRegistrationDto()
        {
            Endpoint = null,
            Topic = null,
        };

        public String Topic { get; set; }
        public String Endpoint { get; set; }
    }
}
