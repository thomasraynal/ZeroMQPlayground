using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public class StateRequest
    {
        public static readonly StateRequest Default = new StateRequest();
        public StateRequest()
        {
            Topic = string.Empty;
        }

        public string Topic { get; set; }
    }
}
