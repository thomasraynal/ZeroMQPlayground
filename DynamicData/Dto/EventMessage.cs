using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Dto
{
    public class EventMessage : IEventMessage
    {
        public IEventId EventId { get; set; }
        public IProducerMessage ProducerMessage { get; set; }
    }
}
