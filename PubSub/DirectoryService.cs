using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public class DirectoryService : Controller, IDirectory
    {
        private IDirectory _directory;

        public DirectoryService(IDirectory directory)
        {
            _directory = directory;
        }

        [HttpGet]
        public async Task<IEnumerable<ProducerDescriptor>> GetStateOfTheWorld()
        {
            return await _directory.GetStateOfTheWorld();
        }

        [HttpGet]
        public async Task<ProducerDescriptor> Next(string topic)
        {
            return await _directory.Next(topic);
        }

        [HttpPut]
        public async Task Register([FromBody] ProducerRegistrationDto producer)
        {
             await _directory.Register(producer);
        }
    }
}
