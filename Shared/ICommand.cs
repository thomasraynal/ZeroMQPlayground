using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface ICommand : IEvent
    {
        Guid CommandId { get; set; }
    }
}
