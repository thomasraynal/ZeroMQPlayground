using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PushPull
{
    public class BusConfiguration
    {
        string Endpoint { get; set; }
        string PeerName { get; set; }
    }
}
