using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.RouterDealer
{
    public class Work
    {
        public static Work Ready = new Work() { Status = WorkerStatus.Ready };

        public WorkerStatus Status { get; set; }
        public Guid ClientId { get; set; }
        public Guid WorkerId { get; set; }
    }
}
