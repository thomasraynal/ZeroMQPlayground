using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using NUnit.Framework;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.PubSub;
using ZeroMQPlayground.PubSub.Events;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.Tests.PubSub
{

    [TestFixture]
    public class TestsPubSub
    {
        [Test]
        public async Task TestDirectory()
        {
            var cancel = new CancellationTokenSource();

            new Task(() =>
            {

                var webHost = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls("http://localhost:8080")
                        .UseStartup<DirectoryStartup>()
                        .Build();

                webHost.Run();


            }, cancel.Token).Start();

        }

        [Test]
        public void TestRoutable()
        {
            var @event = new AccidentEvent()
            {
                Datacenter = "Paris",
                Perimeter = "Business",
                Severity = "Warn"
            };

            var serializer = new EventSerializer();

            var serializedEvent = serializer.Serialize(@event);

            Assert.AreEqual("Paris.Business.Warn", serializedEvent);

        }

            //[Test]
            //public async Task TestNetMQ()
            //{
            //    var cancel = new CancellationTokenSource();

            //    new Task(() =>
            //    {

            //        Random rand = new Random(50);

            //        using (var pubSocket = new PublisherSocket())
            //        {
            //            Console.WriteLine("Publisher socket binding...");
            //            pubSocket.Options.SendHighWatermark = 1000;
            //            pubSocket.Bind("tcp://*:12345");

            //            for (var i = 0; i < 100; i++)
            //            {
            //                var randomizedTopic = rand.NextDouble();
            //                if (randomizedTopic > 0.5)
            //                {
            //                    var msg = "TopicA msg-" + i;
            //                    Console.WriteLine("Sending message : {0}", msg);
            //                    pubSocket.SendMoreFrame("TopicA").SendFrame(msg);
            //                }
            //                else
            //                {
            //                    var msg = "TopicB msg-" + i;
            //                    Console.WriteLine("Sending message : {0}", msg);
            //                    pubSocket.SendMoreFrame("TopicB").SendFrame(msg);
            //                }

            //                Thread.Sleep(500);
            //            }
            //        }



            //    }, cancel.Token).Start();



            //    for(var i=0; i< 10; i++)
            //    {
            //        new Task(() =>
            //        {
            //            Random rand = new Random();
            //            var allowableCommandLineArgs = new[] { "TopicA", "TopicB", "" };

            //            var topic = allowableCommandLineArgs[rand.Next(0, 3)];

            //            Console.WriteLine("Subscriber started for Topic : {0}", topic);

            //            using (var subSocket = new SubscriberSocket())
            //            {
            //                subSocket.Options.ReceiveHighWatermark = 1000;
            //                subSocket.Connect("tcp://localhost:12345");
            //                subSocket.Subscribe(topic);
            //                Console.WriteLine("Subscriber socket connecting...");
            //                while (true)
            //                {
            //                    string messageTopicReceived = subSocket.ReceiveFrameString();
            //                    string messageReceived = subSocket.ReceiveFrameString();
            //                    Console.WriteLine(messageReceived);
            //                }
            //            }

            //        }, cancel.Token).Start();
            //    }


            //   await Task.Delay(5000000);


            //}

        }
}
