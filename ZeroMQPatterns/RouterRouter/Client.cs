using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.RouterRouter
{
    public class Client
    {
        private string _loadBalancerEndpoint;
        private RequestSocket _client;
        private Guid _id;

        public Client(string loadBalancerEndpoint)
        {
            _loadBalancerEndpoint = loadBalancerEndpoint;
            _client = new RequestSocket();

            _client.Connect(_loadBalancerEndpoint);

            _id = Guid.NewGuid();
        }

        public Task<Work> DoWork()
        {

            var work = new Work()
            {
                Status = WorkerStatus.Ask
            };

            var queryBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(work));

            _client.SendFrame(queryBytes);

            if (_client.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(5000), out var responseBytes))
            {
                var response = JsonConvert.DeserializeObject<Work>(Encoding.UTF8.GetString(responseBytes));
                return Task.FromResult(response);
            }

            return Task.FromResult<Work>(null);

        }

        public void Stop()
        {
            _client.Close();
            _client.Dispose();
        }

    }
}
