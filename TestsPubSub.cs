using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NUnit.Framework;
using Refit;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.PubSub;
using ZeroMQPlayground.PubSub.Events;
using ZeroMQPlayground.PubSub.Producers;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.Tests.PubSub
{

    [TestFixture]
    public class TestsPubSub
    {


        [TearDown]
        public void TearDown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        public async Task TestDirectory()
        {
            var cancel = new CancellationTokenSource();
            IWebHost host = null;

            new Task(() =>
            {

                host = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls("http://localhost:8080")
                        .UseStartup<DirectoryStartup>()
                        .Build();

                host.Run();


            }, cancel.Token).Start();


            await Task.Delay(500);

            Assert.IsNotNull(host);

            var api = RestService.For<IDirectory>("http://localhost:8080");
            var topic = "Topic1";

            var producers = await api.GetStateOfTheWorld();

            Assert.IsEmpty(producers);

            await api.Register(new ProducerRegistrationDto()
            {
                Endpoint = "http://localhost:8181",
                Topic = topic
            });

            await api.Register(new ProducerRegistrationDto()
            {
                Endpoint = "http://localhost:8282",
                Topic = topic
            });

            producers = await api.GetStateOfTheWorld();

            Assert.AreEqual(2, producers.Count());

            var first = await api.Next(topic);
            var second = await api.Next(topic);

            Assert.AreNotEqual(first.Endpoint, second.Endpoint);
            Assert.AreEqual(first.Topic, second.Topic);

            cancel.Cancel();

            await host.StopAsync();
            
        }

        [Test]
        public async Task TestProducer()
        {
            var cancel = new CancellationTokenSource();
            SubscriberSocket _subSocket = null;

            var serializer = new EventSerializer();
            var messages = new List<AccidentEvent>();

            var directory = new Directory();
            var configuration = new ProducerConfiguration()
            {
                IsTest = true,
                Endpoint = "tcp://localhost:8080",
                Id = Guid.NewGuid()
            };
            
            var producer = new AccidentProducer(configuration, directory, new JsonSerializerSettings());

            producer.Start();

            await Task.Delay(500);

            new Task(() =>
            {
                using (_subSocket = new SubscriberSocket())
                {
                    _subSocket.Options.ReceiveHighWatermark = 1000;
                    _subSocket.Connect("tcp://localhost:8080");
                    _subSocket.Subscribe("Paris");

                    while (!cancel.IsCancellationRequested)
                    {
                        var topic = _subSocket.ReceiveFrameString();
                        var messageBytes = _subSocket.ReceiveFrameBytes();
                        var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(messageBytes));
                        var message = (AccidentEvent)JsonConvert.DeserializeObject(Encoding.UTF32.GetString(transportMessage.Message), transportMessage.MessageType);
                        messages.Add(message);
                    }
                }

            }, cancel.Token).Start();


            await Task.Delay(1000);

            Assert.Greater(messages.Count, 0);
            Assert.IsTrue(messages.All(m =>
            {
                return serializer.Serialize(m).StartsWith("Paris.");
            }));
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

        [Test]
        public async Task TestE2E()
        {
            var cancel = new CancellationTokenSource();

            var directoryEndpoint = "http://localhost:8080";

            var producer1Endpoint = "tcp://localhost:8181";
            var producer1HeartbeatEndpoint = "tcp://localhost:8282";

            var consumerEndpoint = "tcp://localhost:8383";
            var consumerHeartbeatEndpoint = "tcp://localhost:8484";

            var producer2Endpoint = "tcp://localhost:8585";
            var producer2HeartbeatEndpoint = "tcp://localhost:8686";


            //create directory
            IWebHost host = null;

            new Task(() =>
            {

                host = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls(directoryEndpoint)
                        .UseStartup<DirectoryStartup>()
                        .Build();

                host.Run();


            }, cancel.Token).Start();


            await Task.Delay(500);

            var directory = RestService.For<IDirectory>(directoryEndpoint);

            //create producers
            var configurationProducer1 = new ProducerConfiguration()
            {
                IsTest = true,
                Endpoint = producer1Endpoint,
                HeartbeatEnpoint = producer1HeartbeatEndpoint,
                Id = Guid.NewGuid()
            };
            var configurationProducer2 = new ProducerConfiguration()
            {
                IsTest = true,
                Endpoint = producer2Endpoint,
                HeartbeatEnpoint = producer2HeartbeatEndpoint,
                Id = Guid.NewGuid()
            };


            var producer1 = new AccidentProducer(configurationProducer1, directory, new JsonSerializerSettings());
            var producer2 = new AccidentProducer(configurationProducer2, directory, new JsonSerializerSettings());

            //start only one producer
            producer1.Start();

            await Task.Delay(500);

            var configurationConsumer1 = new ConsumerConfiguration<AccidentEvent>()
            {
                Topic = "Paris.Business",
                Id = Guid.NewGuid(),
                Endpoint = consumerEndpoint,
                HeartbeatEndpoint = consumerHeartbeatEndpoint
            };

            var consumedEvents = new List<AccidentEvent>();

            var consumer = new Consumer<AccidentEvent>(configurationConsumer1, directory, new JsonSerializerSettings());

            consumer.GetSubscription()
                    .Subscribe(ev =>
                    {
                        consumedEvents.Add(ev);
                    });

            //start consumer
            consumer.Start();

            await Task.Delay(1000);

            //the consumer should have fetch and subscribe to a producer
            var stateOfTheWorld = await directory.GetStateOfTheWorld();
            var currentEventCount = consumedEvents.Count;

            //the producer shouold have register to the registry
            var producer = stateOfTheWorld.First();
            Assert.AreEqual(ProducerState.Alive, producer.State);

            //at least an event should have match the filter
            Assert.Greater(currentEventCount, 0);
            Assert.AreEqual(1, stateOfTheWorld.Count());

            //memorize current event count
            var eventCount = consumedEvents.Count;

            //kill the producer
            producer1.Stop();

            await Task.Delay(1000);

            //the directory should have heartbeat the consumer, the consumer should not have consume any more event 
            stateOfTheWorld = await directory.GetStateOfTheWorld();
            producer = stateOfTheWorld.First();

            Assert.AreEqual(ProducerState.NotResponding, producer.State);
            Assert.AreEqual(eventCount, consumedEvents.Count);

            //start the second producer
            producer2.Start();

            await Task.Delay(1000);

            //the directory shoud have register the new producer, the consumer should have subscribe to the new consumer
            stateOfTheWorld = await directory.GetStateOfTheWorld();

            Assert.AreEqual(2, stateOfTheWorld.Count());
            Assert.Greater(consumedEvents.Count, currentEventCount);

            cancel.Cancel();

            producer2.Stop();
            await host.StopAsync();
            consumer.Stop();

        }

        [Test]
        public async Task TestProducerConsumer()
        {
            var cancel = new CancellationTokenSource();

            var directoryEndpoint = "http://localhost:8080";
            var producer1Endpoint = "tcp://localhost:8181";
            var producer1HeartbeatEndpoint = "tcp://localhost:8282";

            IWebHost host = null;

            new Task(() =>
            {

                host = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls(directoryEndpoint)
                        .UseStartup<DirectoryStartup>()
                        .Build();

                host.Run();


            }, cancel.Token).Start();


            await Task.Delay(500);

            var directory = RestService.For<IDirectory>(directoryEndpoint);

            var configurationProducer1 = new ProducerConfiguration()
            {
                IsTest = true,
                Endpoint = producer1Endpoint,
                HeartbeatEnpoint = producer1HeartbeatEndpoint,
                Id = Guid.NewGuid()
            };

            var producer1 = new AccidentProducer(configurationProducer1, directory, new JsonSerializerSettings());
            producer1.Start();

            await Task.Delay(500);

            var configurationConsumer1 = new ConsumerConfiguration<AccidentEvent>()
            {
                Topic = "Paris.Business",
                Id = Guid.NewGuid()
            };

            var consumedEvents = new List<AccidentEvent>();

            var consumer = new Consumer<AccidentEvent>(configurationConsumer1, directory, new JsonSerializerSettings());

            consumer.GetSubscription()
                    .Subscribe(ev =>
                    {
                        consumedEvents.Add(ev);
                    });


            consumer.Start();

            await Task.Delay(500);

            cancel.Cancel();

            await host.StopAsync();

            producer1.Stop();
            consumer.Stop();

            Assert.IsTrue(consumedEvents.Count > 0);

        }

    }
}
