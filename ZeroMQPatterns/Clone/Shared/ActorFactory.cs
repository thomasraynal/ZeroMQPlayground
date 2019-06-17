using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public static class ActorFactory
    {
        public async static Task<Client<TDto>> GetClient<TDto>(
            string name,
            TimeSpan hearbeatDelay,
            TimeSpan hearbeatMaxDelay,
            List<BrokerDescriptor> brokerDescriptors,
            IContainer container)
         where TDto : ISubjectDto
        {
            var configuration = new ClientConfiguration()
            {

                BrokerDescriptors = brokerDescriptors,
                Name = name,
                HearbeatDelay = hearbeatDelay,
                HearbeatMaxDelay = hearbeatMaxDelay,
                Container = container

            };

            var client = new Client<TDto>(configuration);

            await client.Start();

            return client;
        }

        public static Broker<TDto> GetBroker<TDto>(
            string publishUpdatesEndpoint,
            string subscribeToUpdatesEndpoint,
            string sendStateEndpoint,
            string heartbeatEndpoint,
            IContainer container)
        where TDto : ISubjectDto
        {

            var broker = new Broker<TDto>(new BrokerConfiguration()
            {
                PublishUpdatesEndpoint = publishUpdatesEndpoint,
                SubscribeToUpdatesEndpoint = subscribeToUpdatesEndpoint,
                SendStateEndpoint = sendStateEndpoint,
                HeartbeatEndpoint = heartbeatEndpoint,
                Container = container
            });

            broker.Start();

            return broker;

        }
    }
}
