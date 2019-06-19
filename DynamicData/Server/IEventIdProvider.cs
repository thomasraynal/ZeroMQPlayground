using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData
{
    public interface IEventIdProvider
    {
        IEventId Next(string streamName, string subject);
    }
}
