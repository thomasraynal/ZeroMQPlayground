using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public abstract class Actor : IActor
    {
        protected Actor()
        {
            var @interfaces = this.GetType().GetInterfaces().ToList();

            if (@interfaces.Any(@interface => @interface == typeof(IWorker)))
            {
                Type = ActorType.Worker;

            }
            else if (@interfaces.Any(@interface => @interface == typeof(IClient)))
            {
                Type = ActorType.Client;
            }
            else
            {
                Type = ActorType.Gateway;
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public ActorType Type { get; }

        public Heartbeat GetHeartbeat(HeartbeatType type)
        {
            return new Heartbeat()
            {
                Descriptor = GetDescriptor(),
                Type = type
            };
        }

        public ActorDescriptor GetDescriptor()
        {
            return new ActorDescriptor()
            {
                ActorId = Id,
                ActorType = Type
            };
        }

        public abstract Task Start();

        public abstract Task Stop();
    }
}
