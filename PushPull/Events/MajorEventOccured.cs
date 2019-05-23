using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public enum Severity
    {
        Warn,
        Error,
        Fatal
    }

    public class MajorEventOccured : IEvent
    {
        public Severity Severity { get; set; }
        public String Message { get; set; }

        public override string ToString()
        {
            return $"{nameof(MajorEventOccured)} - {Severity} - {Message}";
        }
    }
}
