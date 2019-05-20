using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface ITransportMessage
    {
        String PeerId { get; set; }
        Type MessageType { get; set; }
        byte[] Message { get; set; }
    }
}
