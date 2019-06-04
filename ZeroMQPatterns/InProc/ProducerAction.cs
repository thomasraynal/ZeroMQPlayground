using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.InProc
{
    public class ProducerAction<T> where T : IWork
    {
        public Func<T> Produce { get; set; }
        public Action<T> OnFinished { get; set; }
    }
}
