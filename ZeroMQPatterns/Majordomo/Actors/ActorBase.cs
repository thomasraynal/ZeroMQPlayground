using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public abstract class ActorBase : IActor
    {
        protected ActorBase()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public ActorDescriptor GetDescriptor()
        {
            return new ActorDescriptor()
            {
                ActorId = Id
            };
        }

        public abstract Task Start();

        public abstract Task Stop();
    }
}
