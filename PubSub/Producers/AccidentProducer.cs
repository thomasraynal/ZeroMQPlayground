using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ZeroMQPlayground.PubSub.Events;

namespace ZeroMQPlayground.PubSub.Producers
{
    public class AccidentProducer : ProducerBase<AccidentEvent>
    {
        private readonly List<string> _datacenters = new List<string>() { "Paris", "London", "Tokyo", "Chicago" };
        private readonly List<string> _perimeters = new List<string>() { "Infrastructure", "Business", "Global" };
        private readonly List<string> _severities = new List<string>() { "Warn", "Error", "Fatal" };
        private readonly Random _rand;

        public AccidentProducer(ProducerConfiguration producerConfiguration, IDirectory directory, JsonSerializerSettings settings) : base(producerConfiguration, directory, settings)
        {
            _rand = new Random();
        }

        public override AccidentEvent Next()
        {
            var @event = new AccidentEvent()
            {
                Datacenter = _producerConfiguration.IsTest ? "Paris" : _datacenters[_rand.Next(0, _datacenters.Count)],
                Perimeter = _producerConfiguration.IsTest ? "Business" : _perimeters[_rand.Next(0, _perimeters.Count)],
                Severity = _severities[_rand.Next(0, _severities.Count)]
            };

            return @event;
        }
    }
}
