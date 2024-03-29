﻿using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Logging;
using Stoolball.Teams;

namespace Stoolball.Schools
{
    public class School : IAuditable
    {
        public Guid? SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public string? Website { get; set; }
        public string? Twitter { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }
        public string? YouTube { get; set; }
        public Guid? MemberGroupKey { get; set; }
        public string? MemberGroupName { get; set; }
        public string? SchoolRoute { get; set; }
        public int? UntilYear { get; set; }

        public bool IsActive()
        {
            return !this.UntilYear.HasValue || this.UntilYear.Value > DateTimeOffset.UtcNow.Year;
        }

        public List<Team> ActiveTeams()
        {
            return Teams.Where(x => !x.UntilYear.HasValue || x.UntilYear.Value > DateTimeOffset.UtcNow.Year).ToList();
        }

        public List<Team> Teams { get; internal set; } = new List<Team>();

        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/school/{SchoolId}"); }
        }
    }
}