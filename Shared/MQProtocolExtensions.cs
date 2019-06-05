using NetMQ;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.Shared
{
    public class TansportMessage<T>
    {
        public byte[] SenderId { get; set; }
        public T Message { get; set; }
        public byte[] MessageBytes { get; set; }
    }

    public static class MQProtocolExtensions
    {
        public static TansportMessage<T> GetMessage<T>(this NetMQMessage message)
        {
            var transportMessage = new TansportMessage<T>()
            {
                SenderId = message[0].Buffer,
                MessageBytes = message[2].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }
    }
}
