using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class DefaultUniqueEventIdProvider : IUniqueEventIdProvider
    {
        private long _current = 0;

        public long Next()
        {
            return _current++;
        }
    }
}
