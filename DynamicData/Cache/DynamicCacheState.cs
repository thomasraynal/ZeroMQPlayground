using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData
{
    public enum DynamicCacheState
    {
        Disconnected,
        Staled,
        Connected,
        Disposed
    }
}
