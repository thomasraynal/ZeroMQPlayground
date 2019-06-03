using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.PubSub
{
    public class Directory : IDirectory, IDisposable
    {
        private const string DirectoryHeartbeatEnpoint = "tcp://localhost:9090";

        private const int HeartbeatDelay = 500;

        private readonly ConcurrentDictionary<String, ProducerDescriptor> _stateOfTheWorld;
        private readonly ConfiguredTaskAwaitable _heartbeatTask;
        private CancellationTokenSource _cancel;
        private RouterSocket _heartbeatSocket;

        public Directory()
        {
            _stateOfTheWorld = new ConcurrentDictionary<String, ProducerDescriptor>();
            _cancel = new CancellationTokenSource();
            _heartbeatTask = Task.Run(HeartBeatDirectories, _cancel.Token).ConfigureAwait(false);
        }

        private void HeartBeatDirectories()
        {

            _heartbeatSocket = new RouterSocket(DirectoryHeartbeatEnpoint);

            while (!_cancel.IsCancellationRequested)
            {

                foreach (var producer in _stateOfTheWorld)
                {

                    using (var heartbeatQuery = new RequestSocket(producer.Value.HeartbeatEndpoint))
                    {
                        var heartbeatQueryMessage = new HeartbeatQuery()
                        {
                            HeartbeatEndpoint = DirectoryHeartbeatEnpoint
                        };

                        var msg = Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(heartbeatQueryMessage));

                        heartbeatQuery.SendFrame(msg);

                        Task.Delay(HeartbeatDelay).Wait();

                        var heartbeatMessage = new NetMQMessage();

                        if (_heartbeatSocket.TryReceiveMultipartMessage(ref heartbeatMessage))
                        {
                            producer.Value.State = ProducerState.Alive;
                        }
                        else
                        {
                            producer.Value.State = ProducerState.NotResponding;
                        }
                    }

                }

           
            }
        }

        public Task<IEnumerable<ProducerRegistrationDto>> GetStateOfTheWorld()
        {
            return Task.FromResult(_stateOfTheWorld.Values.Select(producer => new ProducerRegistrationDto()
            {
                Endpoint = producer.Endpoint,
                Topic = producer.Topic,
                HeartbeatEndpoint = producer.HeartbeatEndpoint,
                State = producer.State
            }));
        }

        public Task<ProducerRegistrationDto> Next(string topic)
        {

            var target = _stateOfTheWorld.Values.Where(descriptor => descriptor.Topic == topic && descriptor.State == ProducerState.Alive)
                                   .OrderBy(descriptor => descriptor.LastActivated)
                                   .FirstOrDefault();

            if (null == target) return null;

            target.LastActivated = DateTime.Now;

            return Task.FromResult(new ProducerRegistrationDto()
            {
                Endpoint = target.Endpoint,
                Topic = target.Topic,
                HeartbeatEndpoint = target.HeartbeatEndpoint,
                State = target.State
            });

        }

        public Task Register(ProducerRegistrationDto producer)
        {

            if (!_stateOfTheWorld.ContainsKey(producer.Endpoint))
            {

                var producerDescriptor = new ProducerDescriptor()
                {
                    Endpoint = producer.Endpoint,
                    LastActivated = DateTime.MinValue,
                    State = ProducerState.Alive,
                    Topic = producer.Topic,
                    HeartbeatEndpoint = producer.HeartbeatEndpoint
                };

                _stateOfTheWorld.AddOrUpdate(producer.Endpoint, producerDescriptor, (key, oldValue) =>
                 {
                     return oldValue;
                 });

            }

            return Task.CompletedTask;


        }

        public void Dispose()
        {
            _cancel.Cancel();

            _heartbeatSocket.Close();
            _heartbeatSocket.Dispose();
        }
    }
}
