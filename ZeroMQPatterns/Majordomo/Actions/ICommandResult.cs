﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions
{
    public interface ICommandResult
    {
        Guid WorkerId { get; set; }
    }
}
