using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


namespace ZeroMQPlayground.PushPull
{
    public class MessageInvoker
    {
        public MessageInvoker(DispatchMode mode, Type messageTYpe, IEventHandler handler)
        {
            Mode = mode;
            MessageType = messageTYpe;
            Handler = handler;
        }

        public DispatchMode Mode { get; set; }
        public Type MessageType { get; set; }
        public IEventHandler Handler { get; set; }
    }
}
