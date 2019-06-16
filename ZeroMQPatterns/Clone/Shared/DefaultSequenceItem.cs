using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class DefaultSequenceItem<TDto> : ISequenceItem<TDto> where TDto : ISubjectDto
    {
        public TDto UpdateDto { get; set; }
        public long Position { get; set; }
    }
}
