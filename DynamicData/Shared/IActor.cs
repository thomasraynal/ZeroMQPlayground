using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQPlayground.DynamicData.Shared
{
    public interface IActor
    {
        bool IsStarted { get; }
        Guid Id { get; }
        Task Start();
        Task Stop();
    }
}
