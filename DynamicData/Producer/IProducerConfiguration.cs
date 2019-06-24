using System;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData
{
    public interface IProducerConfiguration : IHeartbeatChecker
    {
        string RouterEndpoint { get; set; }
    }
}