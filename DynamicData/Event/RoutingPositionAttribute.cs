﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.DynamicData.Shared
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RoutingPositionAttribute : Attribute
    {
        public int Position { get; }

        public RoutingPositionAttribute(int position)
        {
            Position = position;
        }
    }
}
