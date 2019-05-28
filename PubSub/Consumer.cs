using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public class Consumer<TEvent> : IConsumer<TEvent>
    {
        private SubscriberSocket _consumerSocket;
        private ConsumerConfiguration<TEvent> _consumerConfiguration;
        private IDirectory _directory;
        private JsonSerializerSettings _settings;
        private CancellationTokenSource _cancel;
        private Subject<TEvent> _subject;
        private ConfiguredTaskAwaitable _consumer;

        public Consumer(ConsumerConfiguration<TEvent> consumerConfiguration, IDirectory directory, JsonSerializerSettings settings)
        {
            _consumerConfiguration = consumerConfiguration;
            _directory = directory;
            _settings = settings;
            _cancel = new CancellationTokenSource();
            _subject = new Subject<TEvent>();
        }

        private void HandleNextConsumer()
        {
            var producer = _directory.Next(typeof(TEvent).ToString()).Result;

            using (_consumerSocket = new SubscriberSocket())
            {
                _consumerSocket.Options.ReceiveHighWatermark = 1000;
                _consumerSocket.Connect(producer.Endpoint);
                _consumerSocket.Subscribe(_consumerConfiguration.Topic);

                while (!_cancel.IsCancellationRequested)
                {
                    var topic = _consumerSocket.ReceiveFrameString();
                    var messageBytes = _consumerSocket.ReceiveFrameBytes();
                    var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(messageBytes));

                    if (transportMessage.MessageType != typeof(TEvent))
                    {
                        throw new InvalidOperationException($"wrong event type {transportMessage.MessageType} vs {typeof(TEvent)}");
                    }

                    var message = (TEvent)JsonConvert.DeserializeObject(Encoding.UTF32.GetString(transportMessage.Message), transportMessage.MessageType);

                    _subject.OnNext(message);
                }
            }
        }


        public IObservable<TEvent> GetSubscription()
        {
            return _subject.AsObservable();
        }

        public void Start()
        {
            _consumer = Task.Run(Consume, _cancel.Token).ConfigureAwait(false);
        }

        public void Consume()
        {
            try
            {
                HandleNextConsumer();
            }
            catch(Exception ex)
            {

            }

        }

        public void Stop()
        {
            _subject.OnCompleted();
            _cancel.Cancel();
            _consumerSocket.Close();
            _consumerSocket.Dispose();
        }
    }
}
