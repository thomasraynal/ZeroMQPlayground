using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    [Route("producers")]
    public class DirectoryService : Controller
    {
        private readonly IDirectory _directory;

        public DirectoryService(IDirectory directory)
        {
            _directory = directory;
        }

        [HttpGet]
        public async Task<IEnumerable<ProducerRegistrationDto>> GetStateOfTheWorld()
        {
            return await _directory.GetStateOfTheWorld();
        }

        [HttpGet("next")]
        public async Task<IActionResult> Next([FromQuery] string topic)
        {
            var next = await _directory.Next(topic);

            if (null == next) return NotFound();

            return Ok(next);
        }

        [HttpPut]
        public async Task<IActionResult> Register([FromBody] ProducerRegistrationDto producer)
        {

            await _directory.Register(producer);

            return StatusCode(201);

        }
    }
}
