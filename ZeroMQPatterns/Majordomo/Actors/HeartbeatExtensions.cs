using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public static class HeartbeatExtensions
    {
        public static Heartbeat GetHeartbeat(this IActor actor, HeartbeatType type)
        {
            return new Heartbeat()
            {
                Descriptor = actor.GetDescriptor(),
                Type = type
            };
        }
    }
}
