using NetMQ;
using NetMQ.Sockets;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground
{
    [TestFixture]
    public class TestRouterDealer
    {

        //[Test]
        //public async Task TestE2E()
        //{
        //    const int delay = 3000; // millis

        //    var clientSocketPerThread = new ThreadLocal<DealerSocket>();

        //    using (var server = new RouterSocket("@tcp://127.0.0.1:5556"))
        //    using (var poller = new NetMQPoller())
        //    {
    
        //        for (int i = 0; i < 3; i++)
        //        {
        //            new Task((state) =>
        //            {
        //                DealerSocket client = null;

        //                if (!clientSocketPerThread.IsValueCreated)
        //                {
        //                    client = new DealerSocket();
        //                    client.Options.Identity =
        //                        Encoding.Unicode.GetBytes(state.ToString());
        //                    client.Connect("tcp://127.0.0.1:5556");
        //                    client.ReceiveReady += Client_ReceiveReady;
        //                    clientSocketPerThread.Value = client;
        //                    poller.Add(client);
        //                }
        //                else
        //                {
        //                    client = clientSocketPerThread.Value;
        //                }

        //                while (true)
        //                {
        //                    var messageToServer = new NetMQMessage();
        //                    //messageToServer.AppendEmptyFrame();
        //                    messageToServer.Append(state.ToString());
        //                    PrintFrames("Client Sending", messageToServer);
        //                    client.SendMultipartMessage(messageToServer);
        //                    Thread.Sleep(delay);
        //                }

        //            }, string.Format("client {0}", i), TaskCreationOptions.LongRunning).Start();
        //        }

        //        // start the poller
        //        poller.RunAsync();

        //        // server loop
        //        while (true)
        //        {
        //            var clientMessage = server.ReceiveMultipartMessage();

        //            PrintFrames("Server receiving", clientMessage);
        //            if (clientMessage.FrameCount == 2)
        //            {
        //                var clientAddress = clientMessage[0];
        //                var clientOriginalMessage = clientMessage[1].ConvertToString();
        //                string response = string.Format("{0} back from server {1}",
        //                    clientOriginalMessage, DateTime.Now.ToLongTimeString());
        //                var messageToClient = new NetMQMessage();
        //                messageToClient.Append(clientAddress);
        //                //messageToClient.AppendEmptyFrame();
        //                messageToClient.Append(response);
        //                server.SendMultipartMessage(messageToClient);
        //            }
        //        }
        //    }
        //}


        //void PrintFrames(string operationType, NetMQMessage message)
        //{
        //    for (int i = 0; i < message.FrameCount; i++)
        //    {
        //        Console.WriteLine("{0} Socket : Frame[{1}] = {2}", operationType, i,
        //            message[i].ConvertToString());
        //    }
        //}

        //void Client_ReceiveReady(object sender, NetMQSocketEventArgs e)
        //{
        //    string result = e.Socket.ReceiveFrameString();
        //    Console.WriteLine("REPLY {0}", result);

        //}
    }
}


