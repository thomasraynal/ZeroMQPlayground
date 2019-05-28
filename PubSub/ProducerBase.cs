using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using ZeroMQPlayground.PubSub.Events;

namespace ZeroMQPlayground.PubSub
{
    public abstract class ProducerBase<TEvent> : IProducer<TEvent>
    {
        protected ProducerConfiguration _producerConfiguration;
        private IDirectory _directory;
        private CancellationTokenSource _cancel;
        private JsonSerializerSettings _settings;
        private ConfiguredTaskAwaitable _producer;
        private PublisherSocket _publisherSocket;
        private Random _rand;

        public ProducerBase(ProducerConfiguration producerConfiguration, IDirectory directory, JsonSerializerSettings settings)
        {
            _producerConfiguration = producerConfiguration;
            _directory = directory;
            _cancel = new CancellationTokenSource();
            _settings = settings;
        }

        public abstract TEvent Next();

        public void Start()
        {
            _directory.Register(new ProducerRegistrationDto()
            {
                Endpoint = _producerConfiguration.ClientEnpoint,
                Topic = typeof(TEvent).ToString()

            }).Wait();

            _producer = Task.Run(Produce, _cancel.Token).ConfigureAwait(false);
            _rand = new Random();

        }

        private void Produce()
        {
            var eventSerializer = new EventSerializer();

            using (_publisherSocket = new PublisherSocket())
            {
                _publisherSocket.Options.SendHighWatermark = 1000;

                _publisherSocket.Bind(_producerConfiguration.Enpoint);

                while (!_cancel.IsCancellationRequested)
                {
                    var next = Next();

                    var topic = eventSerializer.Serialize(next);

                    var message = new TransportMessage()
                    {
                        MessageType = next.GetType(),
                        MessageId = Guid.NewGuid(),
                        Message = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(next, _settings)),
                    };

                    var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(message, _settings));

                    _publisherSocket.SendMoreFrame(topic).SendFrame(msg);

                    Task.Delay(_rand.Next(250, 500)).Wait();

                }

            }
        }

        public void Stop()
        {
            _cancel.Cancel();
            _publisherSocket.Close();
            _publisherSocket.Dispose();
        }
    }
}
