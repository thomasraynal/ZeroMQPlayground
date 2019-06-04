using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Refit;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        private HeartbeatResponse _lastHearbeat;
        private RouterSocket _heartbeatSocket;

        public const int HeartbeatFailureTolerance = 2;
        public const int HeartbeatDelay = 1000;

        public Consumer(ConsumerConfiguration<TEvent> consumerConfiguration, IDirectory directory, JsonSerializerSettings settings)
        {
            _consumerConfiguration = consumerConfiguration;
            _directory = directory;
            _settings = settings;
            _cancel = new CancellationTokenSource();
            _subject = new Subject<TEvent>();
        }


        private bool TryGetNextConsumer()
        {
            try
            {
                if (_cancel.IsCancellationRequested) return false;

                _currentProducer = _directory.Next(typeof(TEvent).ToString()).Result;
            }
            catch (Exception ex)
            {
                //no producer found
                if (ex.InnerException.GetType() == typeof(ApiException)) return false;

                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
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
            _heartbeatSocket.Close();
            _heartbeatSocket.Dispose();
            _consumerSocket.Close();
            _consumerSocket.Dispose();
        }

        public bool HandleHeartbeat()
        {

            var failures = 0;

            _heartbeatSocket = new RouterSocket(_consumerConfiguration.HeartbeatEndpoint);

            while (!_cancel.IsCancellationRequested || failures > HeartbeatFailureTolerance)
            {

                using (var heartbeatQuery = new RequestSocket(_currentProducer.HeartbeatEndpoint))
                {
                    var heartbeat = new HeartbeatQuery()
                    {
                        HeartbeatEndpoint = _consumerConfiguration.HeartbeatEndpoint
                    };

                    var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(heartbeat, _settings));

                    heartbeatQuery.SendFrame(msg);

                    Task.Delay(HeartbeatDelay).Wait();

                    var heartbeatMessage = new NetMQMessage();

                    if (_heartbeatSocket.TryReceiveMultipartMessage(ref heartbeatMessage))
                    {
                        _lastHearbeat = JsonConvert.DeserializeObject<HeartbeatResponse>(Encoding.UTF32.GetString(heartbeatMessage[2].Buffer), _settings);
                        failures = 0;
                    }
                    else
                    {
                        failures++;
                    }

                    if (failures > HeartbeatFailureTolerance)
                    {
                        break;
                    }

                }

            }

            return failures < HeartbeatFailureTolerance;

        }

        public void Consume()
        {
            while (!_cancel.IsCancellationRequested)
            {

                while (!TryGetNextConsumer())
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

        public void Stop()
        {
            _subject.OnCompleted();
            _currentConsumerTaskCancellation.Cancel();
            _cancel.Cancel();

            _heartbeatSocket.Close();
            _heartbeatSocket.Dispose();

            _consumerSocket.Close();
            _consumerSocket.Dispose();
        }
    }
}
