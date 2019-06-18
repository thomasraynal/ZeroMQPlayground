using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class MessageEnveloppe<T>
    {
        public string Topic { get; set; }
        public T Message { get; set; }
        public Type MessageType { get; set; }
    }

    public static class MQProtocolExtensions
    {

        public static void Send<TKey, TAgreggate>(this PublisherSocket publisherSocket, IEvent<TKey, TAgreggate> @event)
           where TAgreggate : IAggregate<TKey>
        {
            //refacto - key serializable
            var enveloppe = new MessageEnveloppe<IEvent<TKey, TAgreggate>>()
            {
                Message = @event,
                Topic = @event.AggregateId.ToString(),
                MessageType = @event.GetType()
            };

            //refacto - key serializable
            publisherSocket
                        .SendMoreFrame(enveloppe.Topic.SerializeString())
                        .SendFrame(enveloppe.Serialize());
        }

        public static MessageEnveloppe<T> Receive<T>(this SubscriberSocket publisherSocket)
        {
            var message = publisherSocket.ReceiveMultipartMessage();
            var enveloppe = message[1].Buffer.Deserialize<MessageEnveloppe<T>>();

            return enveloppe;
        }
    }
}
