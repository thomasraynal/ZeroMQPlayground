using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class BusConfiguration : IBusConfiguration
    {
        public string Endpoint { get; set; }
        public string PeerName { get; set; }
    }
}
