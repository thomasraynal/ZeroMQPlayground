using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actors
{
    public class WorkerDescriptor
    {
        public WorkerDescriptor(byte[] workerId) : this(new Guid(workerId))
        {
        }

        public WorkerDescriptor(Guid workerId)
        {
            WorkerId = workerId;
            LastHeartbeat = DateTime.Now;
        }

        public bool IsAlive(TimeSpan allowedTimeSpan)
        {
            return LastHeartbeat.Add(allowedTimeSpan) < DateTime.Now;
        }

        public Guid WorkerId { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}
