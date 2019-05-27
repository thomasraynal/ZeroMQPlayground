using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public class Directory : IDirectory
    {
        private readonly Dictionary<Guid,ProducerDescriptor> _stateOfTheWorld;

        public Directory()
        {
            _stateOfTheWorld = new Dictionary<Guid, ProducerDescriptor>();
        }

        public Task<IEnumerable<ProducerDescriptor>> GetStateOfTheWorld()
        {
            return Task.FromResult(_stateOfTheWorld.Values.AsEnumerable());
        }

        public Task<ProducerDescriptor> Next(string topic)
        {
            var target = _stateOfTheWorld.Values.Where(descriptor => descriptor.Topic == topic)
                                   .OrderBy(descriptor => descriptor.LastActivated)
                                   .FirstOrDefault();

            return Task.FromResult(target);
        }

        public Task Register(ProducerRegistrationDto producer)
        {
            if (!_stateOfTheWorld.ContainsKey(producer.Id))
            {

                _stateOfTheWorld.Add(producer.Id, new ProducerDescriptor()
                {
                    Endpoint = producer.Endpoint,
                    Id = producer.Id,
                    LastActivated = DateTime.MinValue,
                    State = ProducerState.Alive,
                    Topic = producer.Topic
                });

            }

            return Task.CompletedTask;


        }
    }
}
