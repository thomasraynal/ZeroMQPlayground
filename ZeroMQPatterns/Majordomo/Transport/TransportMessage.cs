using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport
{
    public class TransportMessage
    {

        public TransportMessage()
        {
            MessageId = Guid.NewGuid();
        }

        public T Deserialize<T>()
        {
            return (T)Message.Deserialize(MessageType);
        }

        public Guid CommandId { get; set; }
        public Guid MessageId { get; set; }
        public Guid WorkerId { get; set; }
        public Guid ClientId { get; set; }
        public Type CommandType { get; set; }
        public Type MessageType { get; set; }
        public WorkflowState State { get; set; }
        public byte[] Message { get; set; }
        public bool IsResponse { get; set; }
    }
}
