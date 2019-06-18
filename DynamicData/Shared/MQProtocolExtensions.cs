using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;


namespace ZeroMQPlayground.DynamicData.Shared
{

    public static class MQProtocolExtensions
    {

        public static void Send<TKey, TAgreggate>(this PublisherSocket publisherSocket, IEvent<TKey, TAgreggate> @event)
           where TAgreggate : IAggregate<TKey>
        {
            //todo - key serializable
            //todo - inject serializer
            var enveloppe = new TransportMessage()
            {
                MessageBytes = @event.Serialize(),
                Topic = @event.AggregateId.ToString(),
                MessageType = @event.GetType()
            };

            //refacto - key serializable
            publisherSocket
                        .SendMoreFrame(enveloppe.Topic.SerializeString())
                        .SendFrame(enveloppe.Serialize());
        }

        public static T Receive<T>(this SubscriberSocket publisherSocket)
        {
            var message = publisherSocket.ReceiveMultipartMessage();
            var enveloppe = message[1].Buffer.Deserialize<TransportMessage>();

            return (T)enveloppe.MessageBytes.Deserialize(enveloppe.MessageType);
        }
    }
}
