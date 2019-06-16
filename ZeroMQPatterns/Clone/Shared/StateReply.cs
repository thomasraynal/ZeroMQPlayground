using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.ZeroMQPatterns.Clone
{
    public class StateReply<TDto> where TDto : ISubjectDto
    {
        public List<ISequenceItem<TDto>> Updates { get; set; }
    }
}
