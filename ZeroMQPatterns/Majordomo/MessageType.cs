using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public enum MessageType
    {
        None,
        Ready,
        Ask,
        Finished
    }
}
