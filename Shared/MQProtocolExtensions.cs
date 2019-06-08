using NetMQ;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.Shared
{
    public class TansportMessage<T>
    {
        public TansportMessage()
        {
            Topic = string.Empty;
        }

        public byte[] SenderId { get; set; }
        public String Topic { get; set; }
        public T Message { get; set; }
        public byte[] MessageBytes { get; set; }
    }

    public static class MQProtocolExtensions
    {

        public static TansportMessage<T> GetMessageFromProducer<T>(this NetMQMessage message)
        {
            var transportMessage = new TansportMessage<T>()
            {
                Topic = Encoding.UTF8.GetString(message[0].Buffer),
                MessageBytes = message[1].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }

        public static TansportMessage<T> GetMessageFromDealer<T>(this NetMQMessage message)
        {
            var transportMessage = new TansportMessage<T>()
            {
                SenderId = message[0].Buffer,
                MessageBytes = message[1].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }

        public static TansportMessage<T> GetMessageFromRouter<T>(this NetMQMessage message)
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
