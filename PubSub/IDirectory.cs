using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public interface IDirectory
    {
        Task Register(ProducerRegistrationDto producer);
        Task<ProducerDescriptor> Next(string topic);
        Task<IEnumerable<ProducerDescriptor>> GetStateOfTheWorld();
    }
}
