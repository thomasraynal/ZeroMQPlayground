using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class WorkerDescriptor
    {
        public WorkerDescriptor(byte[] workerId, Type commandType) : this(new Guid(workerId), commandType)
        {
        }

        public WorkerDescriptor(Guid workerId, Type commandType)
        {
            WorkerId = workerId;
            LastHeartbeat = DateTime.Now;
            CommandType = commandType;
        }

        public bool IsAlive(TimeSpan ttl)
        {
            return LastHeartbeat.Add(ttl) > DateTime.Now;
        }

        public Type CommandType { get; set; }
        public Guid WorkerId { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}
