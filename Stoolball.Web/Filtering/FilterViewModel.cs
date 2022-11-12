using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stoolball.Teams;
using Umbraco.Extensions;

namespace Stoolball.Web.Filtering
{
    public class FilterViewModel
    {
        public string? FilteredItemTypePlural { get; set; }
        public string? FilterDescription { get; set; }
        public DateTimeOffset? from { get; set; }
        public DateTimeOffset? to { get; set; }

        private string? _teamRoute;

        [DisplayName("Team")]
        public string? team { get => _teamRoute; set => _teamRoute = value?.TrimStart("/teams/"); }

        [DisplayName("Team")]
        public string? TeamName { get; set; }
        public bool SupportsTeamFilter { get; set; }

        public IEnumerable<Team> Teams { get; set; } = Array.Empty<Team>();
    }
}