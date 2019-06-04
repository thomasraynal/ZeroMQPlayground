using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.InProc
{
    public interface IWork
    {
        Guid ProducerId { get; set; }
    }
}
