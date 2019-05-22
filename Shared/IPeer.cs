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
        List<ISubscription> Subscriptions { get; set; }
    }
}
