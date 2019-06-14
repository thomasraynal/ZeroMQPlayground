using NetMQ;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.Shared
{
    public class MessageEnveloppe<T>
    {
        public MessageEnveloppe()
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

        public static MessageEnveloppe<T> GetMessageFromProducer<T>(this NetMQMessage message)
        {
            var transportMessage = new MessageEnveloppe<T>()
            {
                Topic = Encoding.UTF8.GetString(message[0].Buffer),
                MessageBytes = message[1].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }

        public static MessageEnveloppe<T> GetMessageFromDealer<T>(this NetMQMessage message)
        {
            var transportMessage = new MessageEnveloppe<T>()
            {
                SenderId = message[0].Buffer,
                MessageBytes = message[1].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }

        public static MessageEnveloppe<T> GetMessageFromPublisher<T>(this NetMQMessage message)
        {
            var transportMessage = new MessageEnveloppe<T>()
            {
                Topic = message[0].Buffer.Deserialize<string>(),
                MessageBytes = message[1].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }

        public static MessageEnveloppe<T> GetMessageFromRouter<T>(this NetMQMessage message)
        {
            var transportMessage = new MessageEnveloppe<T>()
            {
                SenderId = message[0].Buffer,
                MessageBytes = message[2].Buffer
            };

            transportMessage.Message = transportMessage.MessageBytes.Deserialize<T>();

            return transportMessage;
        }
    }
}
