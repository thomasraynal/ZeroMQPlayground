using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.RouterRouter
{
    public enum WorkerStatus
    {
        None,
        Ready,
        Ask,
        Finished
    }
}
