using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ReqResp
{
    public class Node
    {
        private readonly string _endpoint;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;
        private ResponseSocket _heartbeat;

        public Node(string endpoint, CancellationToken cancel)
        {
            _endpoint = endpoint;
            _cancel = cancel;
            _proc = Task.Run(Start).ConfigureAwait(false);
        }

        public void Kill()
        {
            _heartbeat.Close();
            _heartbeat.Dispose();
        }

        public void Start()
        {

            using (_heartbeat = new ResponseSocket(_endpoint))
            {
                while (!_cancel.IsCancellationRequested)
                {
                    var messageBytes = _heartbeat.ReceiveFrameBytes();
                    var message = JsonConvert.DeserializeObject<HeartbeatQuery>(Encoding.UTF8.GetString(messageBytes));

                    var response = new HeartbeatResponse()
                    {
                        Endpoint = _endpoint
                    };

                    var responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));

                    _heartbeat.SendFrame(responseBytes);
                }
            }
        }
    }
}
