using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public interface ISubjectDto
    {
        Guid Id { get; set; }
        String Subject { get; set; }
    }
}
