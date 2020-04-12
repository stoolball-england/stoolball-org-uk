using System;
using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class Season
    {
        public int? SeasonId { get; set; }

        public string SeasonName { get; set; }

        public Competition Competition { get; set; }

        public bool IsLatestSeason { get; set; }

        public int StartYear { get; set; }

        public int EndYear { get; set; }

        public string Introduction { get; set; }

        public List<TeamInSeason> Teams { get; internal set; } = new List<TeamInSeason>();

        public string Results { get; set; }

        public bool ShowTable { get; set; }

        public bool ShowRunsScored { get; set; }

        public bool ShowRunsConceded { get; set; }

        public string SeasonRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/season/{SeasonId}"); }
        }
    }
}
