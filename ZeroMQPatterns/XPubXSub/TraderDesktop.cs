﻿using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.ZeroMQPatterns.XPubXSub
{
    public class TraderDesktop
    {
        private string _brokerEndpoint;
        private CancellationToken _cancel;
        private ConfiguredTaskAwaitable _workProc;

        private readonly ConcurrentDictionary<string, Price> _prices;

        public TraderDesktop(string brokerEnpoint, CancellationToken token)
        {
            _brokerEndpoint = brokerEnpoint;
            _cancel = token;

            _prices = new ConcurrentDictionary<string, Price>();

            _workProc = Task.Run(Work, _cancel).ConfigureAwait(false);
        }

        public Price GetLastPrice(string ccyPair)
        {
            return _prices[ccyPair];
        }

        private void Work()
        {
            using (var subscriberSocket = new SubscriberSocket())
            {
                subscriberSocket.Connect(_brokerEndpoint);
                subscriberSocket.Subscribe("FX");

                while (!_cancel.IsCancellationRequested)
                {
                    var message = subscriberSocket.ReceiveMultipartMessage()
                                                    .GetMessageFromProducer<Price>();
                    var price = message.Message;

                    _prices.AddOrUpdate(price.Asset, price, (key,value)=>
                    {
                        return value;
                    });

                }
            }
        }
    }
}
