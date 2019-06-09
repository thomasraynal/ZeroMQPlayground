using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.ParanoidPirate
{
    public class Work
    {
        public static Work Ready = new Work() { MessageType = MessageType.Ready };
        public static Work Ask = new Work() { MessageType = MessageType.Ask };

        public MessageType MessageType { get; set; }
        public Guid ClientId { get; set; }
        public Guid WorkerId { get; set; }
    }
}
