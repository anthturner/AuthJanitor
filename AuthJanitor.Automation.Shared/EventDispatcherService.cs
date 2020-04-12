﻿using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public class EventDispatcherService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IEventSink> _eventSinks;

        public EventDispatcherService(
            ILoggerFactory loggerFactory,
            IEnumerable<IEventSink> eventSinks)
        {
            _logger = loggerFactory.CreateLogger<EventDispatcherService>();
            _eventSinks = eventSinks;
        }

        public Task DispatchEvent(AuthJanitorSystemEvents systemEvent, string source, string eventDetails)
        {
            _logger.LogInformation("Event: {0} from {1} (Detail: {2})", systemEvent, source, eventDetails);
            return Task.WhenAll(_eventSinks.Select(s =>
                s.LogEvent(systemEvent, source, eventDetails)
            ));
        }

        public Task DispatchEvent<TEventObject>(AuthJanitorSystemEvents systemEvent, string source, TEventObject eventObject)
        {
            _logger.LogInformation("Event: {0} from {1} (Object type: {2})", systemEvent, source, typeof(TEventObject));
            return Task.WhenAll(_eventSinks.Select(s =>
                s.LogEvent<TEventObject>(systemEvent, source, eventObject)
            ));
        }
    }
}
