using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.ParanoidPirate
{
    public class Heartbeat
    {
        public static Heartbeat Query = new Heartbeat() { Type = HeartbeatType.Ping };
        public static Heartbeat Response = new Heartbeat() { Type = HeartbeatType.Pong };

        public HeartbeatType Type { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Heartbeat heartbeat &&
                   Type == heartbeat.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type);
        }
    }
}
