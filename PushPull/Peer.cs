using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class Peer : IPeer
    {
        public Peer()
        {
        }

        public Peer(Guid id, string name, string endpoint)
        {
            Id = id;
            Name = name;
            Endpoint = endpoint;
            Subscriptions = new List<ISubscription>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Endpoint { get; set; }
        public List<ISubscription> Subscriptions { get; set; }
    }
}
