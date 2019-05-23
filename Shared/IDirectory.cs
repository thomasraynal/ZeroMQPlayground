﻿using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.PushPull;

namespace ZeroMQPlayground.Shared
{
    public interface IDirectory : IEventHandler<PeerUpdatedEvent>
    {
        IEnumerable<IPeer> GetMatchedPeers(IEvent @event);
    }
}
