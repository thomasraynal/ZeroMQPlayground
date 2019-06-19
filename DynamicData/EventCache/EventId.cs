using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData
{
    //todo : timestamped guid?
    public class EventId : IEventId
    {
        public EventId()
        {
        }

        public EventId(string eventStream, long version, string subject)
        {
            EventStream = eventStream;
            Version = version;
            Subject = subject;
        }

        public string EventStream { get; set; }
        public long Version { get; set; }
        public string Subject { get; set; }
        public string Id => $"{EventStream}.{Version}";

        public override bool Equals(object obj)
        {
            return obj is EventId id &&
                   EventStream == id.EventStream &&
                   Version == id.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EventStream, Version);
        }
    }
}
