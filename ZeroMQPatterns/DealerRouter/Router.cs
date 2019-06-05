using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.DealerRouter
{
    public class Router
    {
        private readonly string _routerEndpoint;
        private readonly string _dealerEndpoint;
        private RouterSocket _router;
        private DealerSocket _dealer;
        private readonly CancellationToken _cancel;
        private readonly ConfiguredTaskAwaitable _proc;

        public Router(string routerEndpoint, string dealerEnpoint, CancellationToken cancel)
        {
            _routerEndpoint = routerEndpoint;
            _dealerEndpoint = dealerEnpoint;
            _cancel = cancel;

            _proc = Task.Run(Start).ConfigureAwait(false);
        }

        public int Handled { get; set; }

        public void Start()
        {
            using (_dealer = new DealerSocket())
            {
                _dealer.Bind(_dealerEndpoint);

                using (_router = new RouterSocket(_routerEndpoint))
                {
                    while (!_cancel.IsCancellationRequested)
                    {
                        var msg = _router.ReceiveMultipartMessage();

                        _dealer.SendFrame(msg[1].Buffer);

                        Handled++;
                    }
                }

            }

            _router.Close();
            _router.Dispose();

            _dealer.Close();
            _dealer.Dispose();
        }
    }
}
