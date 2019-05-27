using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PushPull
{
    public interface ITransportMessage
    {
        Guid MessageId { get; set; }
        Type MessageType { get; set; }
        byte[] Message { get; set; }
        bool IsResponse { get; set; }

    }
}
