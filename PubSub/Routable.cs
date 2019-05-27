using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Routable : Attribute
    {
    }
}
