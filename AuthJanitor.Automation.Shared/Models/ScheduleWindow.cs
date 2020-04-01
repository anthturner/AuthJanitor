﻿using Cronos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthJanitor.Automation.Shared.Models
{
    public class ScheduleWindow : IAuthJanitorModel
    {
        public Guid ObjectId { get; set; }

        public IEnumerable<string> CronStrings { get; set; } = new List<string>();

        [JsonIgnore]
        public DateTimeOffset NextOccurrence =>
            CronStrings.Select(s => CronExpression.Parse(s))
                       .Select(c => c.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Utc).GetValueOrDefault())
                       .OrderBy(c => c.Ticks)
                       .First();
    }
}
