using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public interface ISequenceItem<TDto> where TDto : ISubjectDto
    {
        TDto UpdateDto { get; set; }
        long Position { get; set; }
    }
}
