using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport
{
    public enum Workflow
    {
        None,
        WorkerReady,
        WorkFinished
    }
}
