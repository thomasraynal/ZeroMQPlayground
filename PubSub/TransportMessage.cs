using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class TransportMessage : ITransportMessage
    {
        public TransportMessage()
        {
            MessageId = Guid.NewGuid();
        }

        public Guid MessageId { get; set; }
        public Type MessageType { get; set; }
        public byte[] Message { get; set; }
    }
}
