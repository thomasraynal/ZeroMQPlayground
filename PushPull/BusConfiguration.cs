using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class BusConfiguration : IBusConfiguration
    {
        public bool IsPeerDirectory { get; set; }
        public string DirectoryEndpoint { get; set; }
        public string Endpoint { get; set; }
        public string PeerName { get; set; }
    }
}
