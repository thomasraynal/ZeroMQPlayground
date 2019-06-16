using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class ClientConfiguration
    {
        private int _current;

        public ClientConfiguration()
        {
            _current = 0;
            BrokerDescriptors = new List<BrokerDescriptor>();
        }

        public string Name { get; set; }
        public TimeSpan HearbeatDelay { get; set; }
        public TimeSpan HearbeatMaxDelay { get; set; }

        public List<BrokerDescriptor> BrokerDescriptors { get; set; }

        public BrokerDescriptor Next()
        {
            if (BrokerDescriptors.Count == 0) throw new Exception("no brokers");

            if (_current == BrokerDescriptors.Count) _current = 0;

            return BrokerDescriptors.ElementAt(_current++);
        }
    }
}
