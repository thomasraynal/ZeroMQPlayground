﻿using System;

namespace ZeroMQPlayground.PubSub
{
    public class ConsumerConfiguration<TEvent>
    {
        public Guid Id { get; set; }
        public String Service => typeof(TEvent).ToString();
        public String Topic { get; set; }

        public String Endpoint { get; set; }
        public string HeartbeatEndpoint { get; set; }
    }
}