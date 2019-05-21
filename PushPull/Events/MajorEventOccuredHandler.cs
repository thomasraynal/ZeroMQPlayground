using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.PushPull
{
    public class MajorEventOccuredHandler : IEventHandler<MajorEventOccured>
    {
        private readonly ILogger _logger;

        public MajorEventOccuredHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(MajorEventOccured @event)
        {
            _logger.LogInformation(@event.ToString());
        }
    }
}
