﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.Shared
{
    public interface ICommandResult : IEvent
    {
        Guid CommandId { get; set; }
    }
}
