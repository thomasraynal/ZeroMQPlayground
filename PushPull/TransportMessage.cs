using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{

    public class TransportMessage : ITransportMessage
    {
        public Guid MessageId { get; set; }
        public Type MessageType { get; set; }
        public byte[] Message { get; set; }
        public bool IsResponse { get; set; }
    }
}
