using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;


namespace ZeroMQPlayground.DynamicData.Shared
{

    public static class MQProtocolExtensions
    {
        //public static void Send<TKey, TAgreggate>(this PublisherSocket publisherSocket, IEvent<TKey, TAgreggate> @event)
        //   where TAgreggate : IAggregate<TKey>
        //{

        //    //public TransportMessage Deserialize(ISerializer serializer)
        //    //{
        //    //    var message = Message.Deserialize<TransportMessage>();
        //    //    message.Version = EventId.Version;
        //    //    return message;
        //    //}

           
        //    //todo - inject serializer
        //    var enveloppe = new TransportMessage()
        //    {
        //        MessageBytes = @event.Serialize(),
        //        Subject = @event.Subject,
        //        MessageType = @event.GetType()
        //    };

        //    //refacto - key serializable
        //    publisherSocket
        //                .SendMoreFrame(enveloppe.Subject)
        //                .SendFrame(enveloppe.Serialize());
        //}

        //public static bool TryReceive<T>(this SubscriberSocket publisherSocket, TimeSpan timeout, out T response)
        //{
        //    NetMQMessage message = null;
        //    var hasMessage = publisherSocket.TryReceiveMultipartMessage(timeout, ref message);

        //    if (hasMessage)
        //    {
        //        var enveloppe = message[1].Buffer.Deserialize<TransportMessage>();
        //        response = (T)enveloppe.MessageBytes.Deserialize(enveloppe.MessageType);
        //    }
        //    else
        //    {
        //        response = default;
        //    }

        //    return hasMessage;

        //}
    }
}
