using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public enum DispatchMode
    {
        Synchronous,
        Asynchronous
    }

    public class TransportMessage : ITransportMessage
    {
        public DispatchMode Mode { get; set; }
        public string PeerId { get; set; }
        public Type MessageType { get; set; }
        public byte[] Message { get; set; }
    }
}
