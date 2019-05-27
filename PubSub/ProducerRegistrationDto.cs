using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class ProducerRegistrationDto
    {
        public Guid Id { get; set; }
        public String Topic { get; set; }
        public String Endpoint { get; set; }
    }
}
