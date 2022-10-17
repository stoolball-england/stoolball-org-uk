using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stoolball.Teams;

namespace Stoolball.Web.Filtering
{
    public class FilterViewModel
    {
        public string? FilteredItemTypeSingular { get; set; }
        public string? FilteredItemTypePlural { get; set; }
        public string? FilterDescription { get; set; }
        public DateTimeOffset? from { get; set; }
        public DateTimeOffset? to { get; set; }

        [DisplayName("Team")]
        public Guid? team { get; set; }
        public bool SupportsTeamFilter { get; set; }

        public IEnumerable<Team> Teams { get; set; } = Array.Empty<Team>();
    }
}