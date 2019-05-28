using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public interface IDirectory
    {
        [Put("/producers")]
        Task Register([Body] ProducerRegistrationDto producer);
        [Get("/producers/next")]
        Task<ProducerRegistrationDto> Next(string topic);
        [Get("/producers")]
        Task<IEnumerable<ProducerRegistrationDto>> GetStateOfTheWorld();
    }
}
