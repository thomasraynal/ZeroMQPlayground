using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public enum Perimeter
    {
        Business,
        Infra,
        Global
    }

    public class MinorEventOccured : IEvent
    {
        public Perimeter Perimeter { get; set; }
        public String Message { get; set; }

        public override string ToString()
        {
            return $"{nameof(MinorEventOccured)} - {Perimeter} - {Message}";
        }
    }
}
