using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData
{
    public enum DynamicCacheState
    {
        None,
        Disconnected,
        Staled,
        Connected,
        Reconnected
    }
}
