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

        public long Timestamp { get; set; }

        public override bool Equals(object obj)
        {
            return obj is EventId id &&
                   Subject == id.Subject &&
                   Id == id.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Subject, Id);
        }
    }
}
