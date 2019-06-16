using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public interface IUniqueEventIdProvider
    {
        long Next();
    }
}
