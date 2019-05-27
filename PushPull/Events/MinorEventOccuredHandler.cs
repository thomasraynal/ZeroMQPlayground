using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace ZeroMQPlayground.PushPull
{
    public class MinorEventOccuredHandler : IEventHandler<MinorEventOccured>
    {
        private readonly ILogger _logger;

        public MinorEventOccuredHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(MinorEventOccured @event)
        {
            _logger.LogInformation(@event.ToString());
        }
    }
}
