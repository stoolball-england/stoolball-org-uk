using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stoolball.Teams;

namespace Stoolball.Web.Filtering
{
    public class FilterViewModel
    {
        public string? FilteredItemTypePlural { get; set; }
        public string? FilterDescription { get; set; }
        public bool SupportsDateFilter { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? UntilDate { get; set; }

        public string? TeamRoute { get; set; }

        [DisplayName("Team")]
        public string? TeamName { get; set; }
        public bool SupportsTeamFilter { get; set; }

        public IEnumerable<Team> Teams { get; set; } = Array.Empty<Team>();
    }
}