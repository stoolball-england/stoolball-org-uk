using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stoolball.Teams;
using Umbraco.Extensions;

namespace Stoolball.Web.Filtering
{
    public class FilterViewModel : IFilterParameters
    {
        public string? FilteredItemTypePlural { get; set; }
        public string? FilterDescription { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? UntilDate { get; set; }

        public string? TeamRoute { get; set; }

        [DisplayName("Team")]
        public string? TeamName { get; set; }
        public bool SupportsTeamFilter { get; set; }

        public IEnumerable<Team> Teams { get; set; } = Array.Empty<Team>();
        DateTimeOffset? IFilterParameters.from { get => FromDate; }
        DateTimeOffset? IFilterParameters.to { get => UntilDate; }

        [DisplayName("Team")]
        string? IFilterParameters.team { get => TeamRoute?.TrimStart("/teams/"); }
    }
}