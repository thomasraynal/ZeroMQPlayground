﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Transport
{
    //todo: split up in several descriptors
    public class TransportMessage
    {

        public TransportMessage()
        {
            MessageId = Guid.NewGuid();
            CreationDate = DateTime.Now;
        }
      
        public T Deserialize<T>()
        {
            return (T)Message.Deserialize(MessageType);
        }

        public bool CheckTtl(TimeSpan ttl)
        {
            return CreationDate.Add(ttl) > DateTime.Now;
        }

        public Guid CommandId { get; set; }
        public Guid MessageId { get; set; }
        public Guid WorkerId { get; set; }
        public Guid ClientId { get; set; }
        public Type CommandType { get; set; }
        public Type MessageType { get; set; }
        public Workflow State { get; set; }
        public byte[] Message { get; set; }
        public bool IsResponse { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
