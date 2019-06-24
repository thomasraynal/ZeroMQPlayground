using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData
{
    public enum DynamicCacheState
    {
        NotConnected,
        Disconnected,
        Staled,
        Connected,
        Reconnected
    }
}
