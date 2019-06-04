using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ReqResp
{
    [TestFixture]
    public class TestReqResp
    {
        private HeartbeatResponse HeartBeat(string endpoint)
        {
            using (var heartbeat = new RequestSocket(endpoint))
            {

                var query = new HeartbeatQuery();
                var queryBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(query));

                heartbeat.SendFrame(queryBytes);

                if (heartbeat.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(100), out var responseBytes))
                {
                    var message = JsonConvert.DeserializeObject<HeartbeatResponse>(Encoding.UTF8.GetString(responseBytes));
                    return message;
                }

                return null;

            }
        }

        [Test]
        public async Task TestE2E()
        {
            var cancellation = new CancellationTokenSource();
            var endpoints = Enumerable.Range(0, 10).Select(index => $"tcp://localhost:8{index}8{index}").ToList();
            var nodes = endpoints.Select(index => new Node(index, cancellation.Token)).ToList();

            await Task.Delay(500);

            var heartBeats = endpoints.Select(HeartBeat).Where(hb => hb != null).ToList();

            await Task.Delay(250);

            Assert.AreEqual(endpoints.Count(), heartBeats.Count());

            nodes.Last().Kill();

            heartBeats = endpoints.Select(HeartBeat).Where(hb => hb != null).ToList();

            await Task.Delay(250);

            Assert.AreEqual(endpoints.Count() - 1, heartBeats.Count());
        }
    }
}
