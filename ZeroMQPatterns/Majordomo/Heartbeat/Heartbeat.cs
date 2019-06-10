using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public class Heartbeat
    {

        public static Heartbeat Query(ActorDescriptor actorDescriptor)
        {
            return new Heartbeat() { Type = HeartbeatType.Ping, Descriptor = actorDescriptor };
        }
        public static Heartbeat Response(ActorDescriptor actorDescriptor)
        {
            return new Heartbeat() { Type = HeartbeatType.Ping, Descriptor = actorDescriptor };
        }

        public ActorDescriptor Descriptor { get; set; }

        public HeartbeatType Type { get; set; }

    }
}
