using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class TransportMessage
    {
        public TransportMessage()
        {
            MessageId = Guid.NewGuid();
        }

        public Guid MessageId { get; set; }
        public string Subject { get; set; }
        public byte[] MessageBytes { get; set; }
        public Type MessageType { get; set; }
    }
}
