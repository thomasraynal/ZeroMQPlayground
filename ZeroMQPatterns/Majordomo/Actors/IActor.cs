using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public interface IActor
    {
        Guid Id { get; }

        ActorType Type { get; }

        ActorDescriptor GetDescriptor();
        Heartbeat GetHeartbeat(HeartbeatType type);

        Task Start();
        Task Stop();
    }
}
