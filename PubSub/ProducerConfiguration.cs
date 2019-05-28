using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class ProducerConfiguration
    {
        public bool IsTest { get; set; }
        public Guid Id { get; set; }
        public String Enpoint { get; set; }
        public String ClientEnpoint { get; set; }
    }
}
