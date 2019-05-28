using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public class Directory : IDirectory
    {
        private readonly ConcurrentDictionary<String, ProducerDescriptor> _stateOfTheWorld;

        public Directory()
        {
            _stateOfTheWorld = new ConcurrentDictionary<String, ProducerDescriptor>();
        }

        public Task<IEnumerable<ProducerRegistrationDto>> GetStateOfTheWorld()
        {
            return Task.FromResult(_stateOfTheWorld.Values.Select(producer => new ProducerRegistrationDto()
            {
                Endpoint = producer.Endpoint,
                Topic = producer.Topic
            }));
        }

        public Task<ProducerRegistrationDto> Next(string topic)
        {

            var target = _stateOfTheWorld.Values.Where(descriptor => descriptor.Topic == topic)
                                   .OrderBy(descriptor => descriptor.LastActivated)
                                   .FirstOrDefault();

            if (null == target) return null;

            target.LastActivated = DateTime.Now;

            return Task.FromResult(new ProducerRegistrationDto()
            {
                Endpoint = target.Endpoint,
                Topic = target.Topic
            });

        }

        public Task Register(ProducerRegistrationDto producer)
        {

            if (!_stateOfTheWorld.ContainsKey(producer.Endpoint))
            {

                var producerDescriptor = new ProducerDescriptor()
                {
                    Endpoint = producer.Endpoint,
                    LastActivated = DateTime.MinValue,
                    State = ProducerState.Alive,
                    Topic = producer.Topic
                };

                _stateOfTheWorld.AddOrUpdate(producer.Endpoint, producerDescriptor, (key, oldValue) =>
                 {
                     return oldValue;
                 });

            }

            return Task.CompletedTask;


        }
    }
}
