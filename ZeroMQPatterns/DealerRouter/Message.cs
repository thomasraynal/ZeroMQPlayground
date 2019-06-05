using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.DealerRouter
{
    public class Message
    {
        public long Payload { get; set; }
        public Guid Id { get; set; }
    }
}
