using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public interface ITransportMessage
    {
        Guid MessageId { get; set; }
        Type MessageType { get; set; }
        byte[] Message { get; set; }
    }
}