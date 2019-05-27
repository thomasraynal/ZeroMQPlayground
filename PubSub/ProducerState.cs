using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public enum ProducerState
    {
        None,
        NotResponding,
        Alive
    }
}
