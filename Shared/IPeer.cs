using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface IPeer
    {
        Guid Id { get; set; }
        String Name { get; set; }
        String Endpoint { get; set; }
        IEnumerable<ISubscription> Subscriptions { get; set; }
    }
}
