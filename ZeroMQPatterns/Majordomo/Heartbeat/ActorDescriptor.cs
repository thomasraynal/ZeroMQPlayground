using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo
{
    public class ActorDescriptor
    {
        public Guid ActorId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ActorDescriptor descriptor &&
                   ActorId.Equals(descriptor.ActorId)
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActorId);
        }
    }
}
