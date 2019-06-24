using System.Collections.Generic;
using ZeroMQPlayground.DynamicData.Dto;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface IStateReply
    {
        List<IEventMessage> Events { get; set; }
        string Subject { get; set; }
    }
}