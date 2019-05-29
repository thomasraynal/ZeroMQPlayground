using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
        private ProducerRegistrationDto _currentProducer;
        private CancellationTokenSource _currentConsumerTaskCancellation;
        public const int HeartbeatDelay = 1000;

        public Consumer(ConsumerConfiguration<TEvent> consumerConfiguration, IDirectory directory, JsonSerializerSettings settings)
        {
            _consumerConfiguration = consumerConfiguration;
            _directory = directory;
            _settings = settings;
            _cancel = new CancellationTokenSource();
            _subject = new Subject<TEvent>();
        }

        private bool HandleHeartbeat()
        {
            PushSocket sender = null;
            var hearBeatResult = true;

            try
            {

                sender = new PushSocket();
                sender.Connect(_currentProducer.HeartBeatEndpoint);

                while (!_cancel.IsCancellationRequested)
                {

                    var heartbeat = new HeartbeatQuery()
                    {
                        SenderEndpoint = _consumerConfiguration.Endpoint
                    };

                    var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(heartbeat, _settings));

                    if (!sender.TrySendFrame(msg))
                    {
                        throw new SocketException();
                    }

                    Task.Delay(HeartbeatDelay).Wait();

                }

            }
            catch (Exception ex)
            {
                hearBeatResult = false;
            }
            finally
            {
                sender.Close();
                sender.Dispose();
            }

            return hearBeatResult;
        }

        private bool GetNextConsumer()
        {
            try
            {
                _currentProducer = _directory.Next(typeof(TEvent).ToString()).Result;
            }
            catch (ApiException)
            {
                return false;
            }

            return true;
        }

        private void HandleNextConsumer()
        {
     
            using (_consumerSocket = new SubscriberSocket())
            {
                _consumerSocket.Options.ReceiveHighWatermark = 1000;
                _consumerSocket.Connect(_currentProducer.Endpoint);
                _consumerSocket.Subscribe(_consumerConfiguration.Topic);

                while (!_cancel.IsCancellationRequested && !_currentConsumerTaskCancellation.IsCancellationRequested)
                {
                    var topic = _consumerSocket.ReceiveFrameString();
                    var messageBytes = _consumerSocket.ReceiveFrameBytes();
                    var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(Encoding.UTF32.GetString(messageBytes));

                    if (transportMessage.MessageType != typeof(TEvent))
                    {
                        throw new InvalidOperationException($"wrong event type {transportMessage.MessageType} vs expected {typeof(TEvent)}");
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

        private void RaiseHeartbeatFailed()
        {
            _currentConsumerTaskCancellation.Cancel();
            _consumerSocket.Close();
            _consumerSocket.Dispose();
        }

        public void Consume()
        {
            try
            {
                while (!_cancel.IsCancellationRequested)
                {

                    while (!GetNextConsumer())
                    {
                        Task.Delay(1000).Wait();
                    }

                    _currentConsumerTaskCancellation = new CancellationTokenSource();
                    var currentConsumerTask = new Task(HandleNextConsumer, _currentConsumerTaskCancellation.Token);

                    currentConsumerTask.Start();

                    var heartbeatResult = HandleHeartbeat();

                    if (!heartbeatResult)
                    {
                        RaiseHeartbeatFailed();
                    }

                }
            }
            catch (Exception ex)
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
