using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PushPull
{
    public interface ICommand : IEvent
    {
        Guid CommandId { get; set; }
    }
}
