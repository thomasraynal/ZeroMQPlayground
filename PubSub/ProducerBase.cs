using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
        private ConfiguredTaskAwaitable _heartbeat;
        private PublisherSocket _publisherSocket;
        private Random _rand;
        private PullSocket _heartbeatSocket;

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
                Endpoint = _producerConfiguration.EndpointForClient,
                Topic = typeof(TEvent).ToString(),
                HeartBeatEndpoint = _producerConfiguration.HeartbeatEnpoint

            }).Wait();


            _heartbeat = Task.Run(HandleHeartbeat, _cancel.Token).ConfigureAwait(false);
            _producer = Task.Run(Produce, _cancel.Token).ConfigureAwait(false);

            _rand = new Random();

        }

        private void HandleHeartbeat()
        {
            _heartbeatSocket = new PullSocket(_producerConfiguration.HeartbeatEnpoint);
            
                while (!_cancel.IsCancellationRequested)
            {
                var messageBytes = _heartbeatSocket.ReceiveFrameBytes();
                var heartbeat = JsonConvert.DeserializeObject<HeartbeatQuery>(Encoding.UTF32.GetString(messageBytes), _settings);

                using (var sender = new PushSocket(heartbeat.SenderEndpoint))
                {
                    var heartbeatResponse = new HeartbeatResponse()
                    {
                        ProducerId = _producerConfiguration.Id
                    };

                    var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(heartbeatResponse, _settings));

                    sender.SendFrame(msg);
                }

            }
        }

        private void Produce()
        {
            var eventSerializer = new EventSerializer();

            _publisherSocket = new PublisherSocket();

            _publisherSocket.Options.SendHighWatermark = 1000;

            _publisherSocket.Bind(_producerConfiguration.Endpoint);

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

        public void Stop()
        {
            _cancel.Cancel();

            _heartbeatSocket.Close();
            _heartbeatSocket.Dispose();

            _publisherSocket.Close();
            _publisherSocket.Dispose();



            //try
            //{
            //   NetMQConfig.Cleanup(false);
            //}
            //catch (ObjectDisposedException) { }

        }
    }
}
