using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.RouterRouter
{
    public class Work
    {
        public WorkerStatus Status { get; set; }
        public byte[] ClientId { get; set; }
        public byte[] WorkerId { get; set; }
    }
}
